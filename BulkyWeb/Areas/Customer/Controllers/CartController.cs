using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader = new()
            };

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
                OrderHeader = new()
            };
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;



            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(ShoppingCartVM);
        }
        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPost()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (ModelState.IsValid)
            {
                ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product");

                ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
                ShoppingCartVM.OrderHeader.ApplicationUserId = userId;


                ApplicationUser updateInfoAboutUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

                updateInfoAboutUser.Name = ShoppingCartVM.OrderHeader.Name;
                updateInfoAboutUser.PhoneNumber = ShoppingCartVM.OrderHeader.PhoneNumber;
                updateInfoAboutUser.StreetAddress = ShoppingCartVM.OrderHeader.StreetAddress;
                updateInfoAboutUser.City = ShoppingCartVM.OrderHeader.City;
                updateInfoAboutUser.State = ShoppingCartVM.OrderHeader.State;
                updateInfoAboutUser.PostalCode = ShoppingCartVM.OrderHeader.PostalCode;

                _unitOfWork.ApplicationUser.Update(updateInfoAboutUser);





                ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);


                foreach (var cart in ShoppingCartVM.ShoppingCartList)
                {
                    cart.Price = GetPriceBasedOnQuantity(cart);
                    ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
                }
                if (applicationUser.CompanyId.GetValueOrDefault() == 0)
                {
                    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                    ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
                }
                else
                {

                    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                    ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
                }
                _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
                _unitOfWork.Save();

                foreach (var cart in ShoppingCartVM.ShoppingCartList)
                {
                    OrderDetail orderDetail = new()
                    {
                        ProductId = cart.ProductId,
                        OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                        Price = cart.Price,
                        Count = cart.Count
                    };
                    _unitOfWork.OrderDetail.Add(orderDetail);
                    _unitOfWork.Save();
                }

                if (applicationUser.CompanyId.GetValueOrDefault() == 0)
                {
                    //STRIKE LOGIC
                    StripeConfiguration.ApiKey = "sk_test_51Ngmr8Cm0pEsqbyjNGjLf6xrFi4g56gHJdqQ6g3Y9nHvI5BZoqOzEMeGCb08qk7aoZ2R9l9vrIExdfZoZ9BMZj5S00lBlzmJSX";
                    var domain = "https://localhost:7169/";
                    var options = new SessionCreateOptions
                    {
                        SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
                        CancelUrl = domain + "customer/cart/index",
                        LineItems = new List<SessionLineItemOptions>(),
                        Mode = "payment",
                    };

                    foreach (var item in ShoppingCartVM.ShoppingCartList)
                    {
                        var sessionLineItem = new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions()
                            {
                                UnitAmount = (long)(item.Price * 100),
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = item.Product.Title,

                                }
                            },
                            Quantity = item.Count
                        };
                        options.LineItems.Add(sessionLineItem);

                    }

                    var service = new SessionService();
                    Session session = service.Create(options);
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);

                    _unitOfWork.Save();

                    Response.Headers.Add("Location", session.Url);
                    return new StatusCodeResult(303);


                }
                

                return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
            }
            else
            {
                
                return Index();
            }


        }

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);


                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
                HttpContext.Session.Clear();

            }

            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();

            return View(id);
        }
        public IActionResult Plus(int cartId)
        {
            var cardFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            cardFromDb.Count += 1;
            _unitOfWork.ShoppingCart.Update(cardFromDb);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }
        public IActionResult Minus(int cartId)
        {
            var cardFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId,tracked:true);
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (cardFromDb.Count <= 1)
            {

                HttpContext.Session.SetInt32(SD.SessionCart,
                      _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cardFromDb.ApplicationUserId).Count() - 1);
                _unitOfWork.ShoppingCart.Remove(cardFromDb);
            }
            else
            {
                cardFromDb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cardFromDb);
            }



            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }
        public IActionResult Remove(int cartId)
        {
            var cardFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked: true);

            HttpContext.Session.SetInt32(SD.SessionCart,
                       _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cardFromDb.ApplicationUserId).Count()-1);
            _unitOfWork.ShoppingCart.Remove(cardFromDb);
            _unitOfWork.Save();
           
            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else if (shoppingCart.Count <= 100)
            {
                return shoppingCart.Product.Price50;
            }
            else
            {
                return shoppingCart.Product.Price100;
            }

        }
    }
}
