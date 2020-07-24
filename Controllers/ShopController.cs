using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using georgianComputers.Models;
using Microsoft.AspNetCore.Mvc;

namespace georgianComputers.Controllers
{
    public class ShopController : Controller
    {
        //ADD db conection
        private readonly GeorgianComputersContext _context;

        public ShopController(GeorgianComputersContext context)
        {
            _context = context; 
        }
        //Get: /Shop
        public IActionResult Index()
        {
            var categories = _context.Category.OrderBy(c => c.Name).ToList();
            return View(categories);
        }
        //GET: /browse/catName
        public IActionResult Browse(string category)
        {
            //store the selctes category name in the viewbag so we can display in the view heading
            ViewBag.Category = category;

            // get the list of products for the slected category and pass the list to the view
            var products = _context.Product.Where(p => p.Category.Name == category).OrderBy(p => p.Name).ToList();
            return View(products);
        }

        //GET: /ProductDetails/prodName
        public IActionResult ProductDetails(String product)
        {
            //Use a singleOrDefault to find either 1 exact match or a null object
            var selectedProduct = _context.Product.SingleOrDefault(p => p.Name == product);
            return View(selectedProduct);
        }

        //POST: AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int Quantity, int ProductId)
        {
            //identify product Price
            var product = _context.Product.SingleOrDefault(p => p.ProductId == ProductId);
            var price = product.Price;
            //create and save a new cart object
            var cart = new Cart
            {
                ProductId = ProductId,
                Quantity = Quantity,
                Price = price,
                Username = "tempUser"
            };

            _context.Cart.Add(cart);
            _context.SaveChanges();
            //show the cart page
            return RedirectToAction("Cart");


           
        }


    }
}