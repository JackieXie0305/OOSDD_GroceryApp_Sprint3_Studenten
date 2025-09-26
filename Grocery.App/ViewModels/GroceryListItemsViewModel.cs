using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Grocery.App.ViewModels
{
    [QueryProperty(nameof(GroceryList), nameof(GroceryList))]
    public partial class GroceryListItemsViewModel : BaseViewModel
    {
        private readonly IGroceryListItemsService _groceryListItemsService;
        private readonly IProductService _productService;
        private readonly IFileSaverService _fileSaverService;

        public ObservableCollection<GroceryListItem> MyGroceryListItems { get; set; } = [];
        public ObservableCollection<Product> AvailableProducts { get; set; } = [];

        [ObservableProperty]
        string searchTerm;

        public ObservableCollection<Product> FilteredProducts { get; set; } = [];

        public IRelayCommand SearchCommand { get; }

        [ObservableProperty]
        GroceryList groceryList = new(0, "None", DateOnly.MinValue, "", 0);
        [ObservableProperty]
        string myMessage;
        [ObservableProperty]
        string listSearchTerm;

        public ObservableCollection<GroceryListItem> FilteredGroceryListItems { get; set; } = [];

        public IRelayCommand ListSearchCommand { get; }

        public GroceryListItemsViewModel(IGroceryListItemsService groceryListItemsService, IProductService productService, IFileSaverService fileSaverService)
        {
            _groceryListItemsService = groceryListItemsService;
            _productService = productService;
            _fileSaverService = fileSaverService;

            ListSearchCommand = new RelayCommand<string>(OnListSearch);

            Load(groceryList.Id);
        }

        private void OnListSearch(string searchText)
        {
            ListSearchTerm = searchText;
            FilterGroceryListItems();
        }

        private void FilterGroceryListItems()
        {
            FilteredGroceryListItems.Clear();
            var filtered = string.IsNullOrWhiteSpace(ListSearchTerm)
                ? MyGroceryListItems
                : new ObservableCollection<GroceryListItem>(
                    MyGroceryListItems.Where(item =>
                        item.Product.Name.Contains(ListSearchTerm, StringComparison.OrdinalIgnoreCase))
                  );

            foreach (var item in filtered)
                FilteredGroceryListItems.Add(item);
        }

        private void Load(int id)
        {
            MyGroceryListItems.Clear();
            foreach (var item in _groceryListItemsService.GetAllOnGroceryListId(id))
                MyGroceryListItems.Add(item);
            GetAvailableProducts();
            FilterGroceryListItems(); // Filter direct na laden
        }

        private void GetAvailableProducts()
        {
            AvailableProducts.Clear();
            foreach (Product p in _productService.GetAll())
                if (MyGroceryListItems.FirstOrDefault(g => g.ProductId == p.Id) == null && p.Stock > 0)
                    AvailableProducts.Add(p);

            FilterProducts(); 
        }

        private void FilterProducts()
        {
            FilteredProducts.Clear();
            var filtered = string.IsNullOrWhiteSpace(SearchTerm)
                ? AvailableProducts
                : new ObservableCollection<Product>(
                    AvailableProducts.Where(p => p.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                  );

            foreach (var product in filtered)
                FilteredProducts.Add(product);
        }

        private void OnSearch(string searchText)
        {
            SearchTerm = searchText;
            FilterProducts();
        }

        partial void OnGroceryListChanged(GroceryList value)
        {
            Load(value.Id);
        }

        [RelayCommand]
        public async Task ChangeColor()
        {
            Dictionary<string, object> paramater = new() { { nameof(GroceryList), GroceryList } };
            await Shell.Current.GoToAsync($"{nameof(ChangeColorView)}?Name={GroceryList.Name}", true, paramater);
        }

        [RelayCommand]
        public void AddProduct(Product product)
        {
            if (product == null) return;
            GroceryListItem item = new(0, GroceryList.Id, product.Id, 1);
            _groceryListItemsService.Add(item);
            product.Stock--;
            _productService.Update(product);
            AvailableProducts.Remove(product);
            FilterProducts();
            OnGroceryListChanged(GroceryList);      
        }

        [RelayCommand]
        public async Task ShareGroceryList(CancellationToken cancellationToken)
        {
            if (GroceryList == null || MyGroceryListItems == null) return;
            string jsonString = JsonSerializer.Serialize(MyGroceryListItems);
            try
            {
                await _fileSaverService.SaveFileAsync("Boodschappen.json", jsonString, cancellationToken);
                await Toast.Make("Boodschappenlijst is opgeslagen.").Show(cancellationToken);
            }
            catch (Exception ex)
            {
                await Toast.Make($"Opslaan mislukt: {ex.Message}").Show(cancellationToken);
            }
        }
    }
}
