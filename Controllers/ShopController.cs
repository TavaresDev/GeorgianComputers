using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using georgianComputers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            //Determine Username
            var cartUsername = GetCartUserName();


            //create and save a new cart object
            var cart = new Cart
            {
                ProductId = ProductId,
                Quantity = Quantity,
                Price = price,
                Username = cartUsername
            };

            _context.Cart.Add(cart);
            _context.SaveChanges();
            //show the cart page
            return RedirectToAction("Cart");

        }
        //check or set cart username
        private string GetCartUserName()
        {
            //1. check if we are already stores with username in user session
            if(HttpContext.Session.GetString("CartUserName") == null)
            {
                //Initialize an empty string variable that will latter add to the session object
                var cartUsername = "";

                //2. if no, username in session there are no items in the cart yet, is user logged in?
                //if yes, use their emailfor sesseion variable
                if (User.Identity.IsAuthenticated)
                {
                    cartUsername = User.Identity.Name;
                }
                else
                {
                    //if no, user GUID class to make a new ID and Store That in the session
                    cartUsername = Guid.NewGuid().ToString();

                }
                //next, store the carUsername in a session var
                HttpContext.Session.SetString("CartUserName", cartUsername);

            }
            //send back the username
            return HttpContext.Session.GetString("CartUserName");
        }

        public IActionResult Cart()
        {
            //1. figuere out who the user is
            var cartUserName = GetCartUserName();

            //2. Query the DB to get the user cart items
            var cartItems = _context.Cart.Include(c=> c.Product).Where(c => c.Username == cartUserName).ToList();

            //3. Load a view to pass the cart items for display
            return View(cartItems);
        }

        public IActionResult RemoveFromCart(int id)
        {
            // get the object the user wants to delete 
            var cartItem = _context.Cart.SingleOrDefault(c => c.CartId == id);
            //delete the obj
            _context.Cart.Remove(cartItem);
            _context.SaveChanges();

            //redirect to update cart
            return RedirectToAction("Cart");
        }

        [Authorize]
        public IActionResult Checkout() //Set
        {
            //check if the user has been shoping anonymously now that they are logged in
            Migratecart();
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout([Bind("FirstName, LastName, Address, City, Province, PostalCode, Phone")] Order order) //Get
        {
            //autofill the date, userm and total properties insted of asking the user
            order.OrderDate = DateTime.Now;
            order.UserId = User.Identity.Name;

            var cartItems = _context.Cart.Where(c => c.Username == User.Identity.Name);
            decimal cartTotal = (from c in cartItems
                                 select c.Quantity * c.Price).Sum();

            order.Total = cartTotal;
            // will need an EXTENCION to the .Net COre Session Object to store the order Object = in the next video
            HttpContext.Session.SetString("cartTotal", cartTotal.ToString());

            return RedirectToAction("Payment");


        }

        private void Migratecart()
        {
            //if user has shoppen without an account, attach theys item to their username
            if(HttpContext.Session.GetString("CartUsername") != User.Identity.Name)
            {
                var cartUsername = HttpContext.Session.GetString("CartUsername");
                //get the user name
                var cartItems = _context.Cart.Where(c => c.Username == cartUsername);
                //loop through the cart items and update the username for each one
                foreach(var item in cartItems)
                {
                    item.Username = User.Identity.Name;
                    _context.Update(item);
                }
                _context.SaveChanges();

                //update the session variable from a GUID to the user email
                HttpContext.Session.SetString("CartUsername", User.Identity.Name);
            }
        }
    }
}