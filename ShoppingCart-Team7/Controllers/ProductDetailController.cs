﻿using Microsoft.AspNetCore.Mvc;
using ShoppingCart_Team7.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using ShoppingCart_Team7.Controllers;

namespace ShoppingCart_Team7.Controllers
{
    public class ProductDetailController : Controller
    {
        private DBContext dbContext;
        public ProductDetailController(DBContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IActionResult Index(Guid Id)
        {
            // get product detail from DB product table
            Product product = GetAllDetails(Id);
            ViewData["product"] = product;

            // get rating count and average rating from DB reviews table(LINQ)
            List<Review> allreviewList = dbContext.Reviews.ToList();
            var ratingsPerProduct = allreviewList.GroupBy(x => x.ProductId);
            int arating = 0;
            int arating_num = 0;
            foreach (var grp in ratingsPerProduct)
                if (grp.Key == Id)
                {
                    arating = (int)Math.Round(grp.Average(x => x.Rating));
                    arating_num = grp.Count();
                }

            ViewData["rating_num"] = arating_num;
            ViewData["rating"] = arating;

            // get per review detail from DB reviews table
            List<Review> aReviewList = dbContext.Reviews.Where(x => x.ProductId == Id).ToList();

            List<string> userNameList = new List<string>();

            foreach (var i in aReviewList)
            {
                userNameList.Add(GetReviewUser(i.Id)[0].ToString().ToUpper() + GetReviewUser(i.Id).Substring(1));
            }
            

            ViewData["userNameList"] = userNameList;
            ViewData["aReviewList"] = aReviewList;

            // get recommendation
            ViewData["Recommendation"] = GetRecommendations(Id);

            return View();
        }

        // Get AllDetail funtion
        protected static readonly string connectionString = "Server=localhost;Database=ShoppingCartDB; Integrated Security=true";
        public static Product GetAllDetails(Guid Id)
        {
            Product product = null;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"select Products.Id, Products.ProductName, Products.Price, Products.Description, Products.ImageSrc, Products.Category from Products
                    where Products.Id = '" + Id + "'";

                Debug.WriteLine(sql);
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    product = new Product()
                    {
                        ProductName = (string)reader["ProductName"],
                        Price = (float)reader["Price"],
                        ImageSrc = (string)reader["ImageSrc"],
                        Category = (string)reader["Category"],
                        Description = (string)reader["Description"],
                        Id = (Guid)reader["Id"]
                    };
                };
            }
            return product;
        }

        // GetReviewUser
        public string GetReviewUser(Guid ReviewId)
        {
            string userName = "";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"select Users.UserName from Purchases, users, Reviews 
                    where Reviews.PurchasesId = Purchases.Id
                    and Purchases.UserId = Users.Id
                    and Reviews.Id =  '" + ReviewId + "'";

                Debug.WriteLine(sql);
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    userName = (string)reader["UserName"];
                };
            }
            return userName;
        }

        // GetRecommendations function
        public List<Product> GetRecommendations(Guid Id)
        {
            Random r = new Random();
            List<Product> Recommendations = new List<Product>();
            List<Purchase> PastPurchases = new List<Purchase>();

            int SelectionCount = 4; // Number of recommendations to return

            // Find list of purchases with the same Product ID
            List<Purchase> SalesWithSameProductId = dbContext.Purchases.Where(x => x.ProductId == Id).ToList();

            // If no one has bought this item before, update selectionCount so it won't return a recommendation list
            if (SalesWithSameProductId.Count() == 0)
            {
                SelectionCount = 0;
                return Recommendations;
            }

            // Get distinct users who bought the same book before
            List<Guid> Users = (from purchase in SalesWithSameProductId
                                select purchase.UserId).Distinct().ToList();

            // Get all past purchases made by users who bought the same book
            foreach (Guid user in Users)
            {
                List<Purchase> UserPurchase = dbContext.Purchases.Where(x => x.UserId == user && x.ProductId != Id).ToList();

                // Append list of Purchases to
                PastPurchases.AddRange(UserPurchase);
            }

            // Get distinct Product Id from Past Purchases
            List<Guid> ProductIds = (from pur in PastPurchases
                                     select pur.ProductId).Distinct().ToList();

            // Change SelectionCount according to ProductIds count if ProductIds count < 4
            if (ProductIds.Count() < 4)
                SelectionCount = ProductIds.Count();

            // Generate recommendations
            List<int> BookIndex = new List<int>();

            // Choose n number of random books based on Selection count
            for (int i = 0; i < SelectionCount; i++)
            {
                bool check = true;
                while (check)
                {
                    int index = r.Next(ProductIds.Count());
                    if (!BookIndex.Contains(index))
                    {
                        BookIndex.Add(index);
                        check = false;
                    }
                }
            }

            // Add books into Recommendation List
            foreach (int i in BookIndex)
            {
                Recommendations.Add(dbContext.Products.FirstOrDefault(x => x.Id == ProductIds[i]));
            }

            return Recommendations;
        }
    }
}
