using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("customer")]
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;


        public ReviewController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index(int productId)
        {
            
            Product Product = _unitOfWork.Product.Get(u => u.Id == productId, includeProperties: "Review");
            foreach(var review in Product.Review)
            {
                review.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == review.ApplicationUserId);
            }


            return View(Product);
        }
        [HttpPost]
        [Authorize]
        public IActionResult InputReview(int productId,string reviewText)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            Product ProductL = _unitOfWork.Product.Get(u => u.Id == productId, includeProperties: "Review");

            if(reviewText == null)
            {
                TempData["error"] = "The text Review is Empty";
                return RedirectToAction(nameof(Index), new { productId = ProductL.Id });
            }
            Review Review = new()
            {
                Text = reviewText,
                ProductId = ProductL.Id,
                DateOfReview = DateTime.Now,
                ApplicationUserId = userId
            };


            foreach(var review in ProductL.Review)
            {
                if(review.ApplicationUserId == userId)
                {
                    TempData["error"] = "You already wrote the review";
                    return RedirectToAction(nameof(Index), new { productId = ProductL.Id });
                }
            }
            if (ProductL.Review == null)
            {
                ProductL.Review = new List<Review>();
            }


            _unitOfWork.Review.Add(Review);
            ProductL.Review.Add(Review);
         
           
            _unitOfWork.Product.Update(ProductL);
            _unitOfWork.Save();
            TempData["success"] = "Review written successfully";
            return RedirectToAction(nameof(Index), new { productId = ProductL.Id}) ;
        }

   
       
        public IActionResult DeleteReview(int reviewId)
        {
            Review ReviewToBeDeleted = _unitOfWork.Review.Get(u=>u.Id == reviewId);

            if(ReviewToBeDeleted != null)
            {
                _unitOfWork.Review.Remove(ReviewToBeDeleted);
                _unitOfWork.Save();
            }

            return RedirectToAction(nameof(Index), new { productId = ReviewToBeDeleted.ProductId });
        }
    }
}
