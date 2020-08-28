using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using georgianComputers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace georgianComputers.Controllers
{
    public class ShopController : Controller
    {
        //ADD db conection. the _context is a convention becaus ethis is been injected
        private readonly GeorgianComputersContext _context;

        // Add config so controller can real config value asssetings.json
        private IConfiguration _configuration;

        public ShopController(GeorgianComputersContext context, IConfiguration configuration) // dependency injection
        {
            //accept an intance of our DB connection class and use this object connection
            _context = context;

            //accept an intance of the configuration object se we can read appsettings.json
            _configuration = configuration;
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

        //Get: /Shop
        [Route("api/Categories")]
        public IActionResult GetCategories()
        {
            var categories = _context.Category.OrderBy(c => c.Name).ToList();
            return Json(new { categories });
        }


        [Route("api/Categories/{catId:int}")]
        public IActionResult GetProductsByCategory(int catId)
        {
            //store the selctes category name in the viewbag so we can display in the view heading
            ViewBag.Category = catId;

            // get the list of products for the slected category and pass the list to the view
            var products = _context.Product.Where(p => p.Category.CategoryId == catId).OrderBy(p => p.Name).ToList();
            return Json(products);
        }

        //GET: /ProductDetails/prodName
        public IActionResult ProductDetails(String product)
        {
            //Use a singleOrDefault to find either 1 exact match or a null object
            // if there is 2 products with same name, it breaks change to  FirstOrDefault()
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

            //Check if this user's products already exists in the cart. if so, update the quantity

            var cartItem = _context.Cart.SingleOrDefault(c => c.ProductId == ProductId && c.Username == cartUsername);
            if (cartItem == null)
            {

                //create and save a new cart object
                var cart = new Cart
                {
                    ProductId = ProductId,
                    Quantity = Quantity,
                    Price = price,
                    Username = cartUsername
                };
                _context.Cart.Add(cart);
            }
            else
            {
                cartItem.Quantity += Quantity;
                _context.Update(cartItem);

            }

            _context.SaveChanges();
            //show the cart page
            return RedirectToAction("Cart");

        }
        //check or set cart username
        private string GetCartUserName()
        {
            //1. check if we are already stores with username in user session
            if (HttpContext.Session.GetString("CartUserName") == null)
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
            var cartItems = _context.Cart.Include(c => c.Product).Where(c => c.Username == cartUserName).ToList();

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
            MigrateCart();
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout([Bind("FirstName, LastName, Address, City, Province, PostalCode, Phone")] Models.Order order) //Get
        {
            //autofill the date, userm and total properties insted of asking the user
            order.OrderDate = DateTime.Now;
            order.UserId = User.Identity.Name;

            var cartItems = _context.Cart.Where(c => c.Username == User.Identity.Name);
            decimal cartTotal = (from c in cartItems
                                 select c.Quantity * c.Price).Sum();

            order.Total = cartTotal;
            // will need an EXTENCION to the .Net COre Session Object to store the order Object = in the next video
            //HttpContext.Session.SetString("cartTotal", cartTotal.ToString());

            // we have the session to complex object
            HttpContext.Session.SetObject("Order", order);

            return RedirectToAction("Payment");

        }

      
        private void MigrateCart()
        {
            //if user has shoppen without an account, attach theys item to their username
            if (HttpContext.Session.GetString("CartUsername") != User.Identity.Name)
            {
                var cartUsername = HttpContext.Session.GetString("CartUsername");
                //get the user name
                var cartItems = _context.Cart.Where(c => c.Username == cartUsername);
                //loop through the cart items and update the username for each one
                foreach (var item in cartItems)
                {
                    item.Username = User.Identity.Name;
                    _context.Update(item);
                }
                _context.SaveChanges();

                //update the session variable from a GUID to the user email
                HttpContext.Session.SetString("CartUsername", User.Identity.Name);
            }
        }

        [Authorize]
        public IActionResult Payment()
        {
            //setup payment page to show order total

            // 1. Get the order from The session variable & cast as an order Object
            var order = HttpContext.Session.GetObject<Models.Order>("Order");

            //2. Use viewbag to display total and pass the amount to Strip
            ViewBag.Total = order.Total;
            //Strip dont like descimals, so:
            ViewBag.CentsTotal = order.Total * 100;
            ViewBag.PublishableKey = _configuration.GetSection("Stripe")["PublishableKey"];

            return View();
        }
        // Need to get 2 things from stripe after authorization
        //overloading method 
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Payment(string stripeEmail, string stripeToken)
        {
            //send payment to stripe
            StripeConfiguration.ApiKey = _configuration.GetSection("Stripe")["SecretKey"];
            var cartUsername = HttpContext.Session.GetString("CartUsername");
            var cartItems = _context.Cart.Where(c => c.Username == cartUsername);
            var order = HttpContext.Session.GetObject<Models.Order>("Order");



            //new stripe payment attempt
            var customerService = new CustomerService();
            var chargeService = new ChargeService();
            // new customer email from payment form, token auto-generated on payment form also
            var customer = customerService.Create(new CustomerCreateOptions
            {
                Email = stripeEmail,
                Source = stripeToken
            });
            //there is cut in the video at 1:13:35
            //new charge using customer created above
            var charge = chargeService.Create(new ChargeCreateOptions
            {
                Amount = Convert.ToInt32(order.Total * 100),
                Description = "Andre Store Purchase",
                Currency = "cad",
                Customer = customer.Id
            });

            //generate and save new order
            _context.Order.Add(order);
            _context.SaveChanges();

            //save order datail
            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };
                _context.OrderDetail.Add(orderDetail);

            }
            _context.SaveChanges();

            //delete the cart
            foreach (var item in cartItems)
            {
                _context.Cart.Remove(item);

            }
            _context.SaveChanges();

            //confirm with a receipt for the new OrderId

            return RedirectToAction("Details", "Orders", new { id = order.OrderId });

        }
    }
}