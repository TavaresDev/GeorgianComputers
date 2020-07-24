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


    }
}