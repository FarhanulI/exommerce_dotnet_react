using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities.OrderAggregate;
using API.Extensions;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace API.Controllers
{
    // Define enum for charge status
    public enum ChargeStatus
    {
        Succeeded,
        Pending,
        Failed,
        // Add more status values as needed
    }

    public class PaymentsController : BaseApiController
    {
        private readonly PaymentService _paymentService;
        private readonly StoreContext _context;
        private readonly IConfiguration _config;

        // Constructor to inject dependencies
        public PaymentsController(PaymentService paymentService, StoreContext context, IConfiguration config)
        {
            _config = config;
            _context = context;
            _paymentService = paymentService;
        }

        // Action to create or update payment intent
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<BasketDto>> CreateOrUpdatePaymentIntent()
        {
            // Retrieve the basket for the authenticated user
            var basket = await _context.Baskets
                .RetrieveBasketWithItems(User.Identity.Name)
                .FirstOrDefaultAsync();

            if (basket == null)
                return NotFound();

            // Create or update payment intent using PaymentService
            var intent = await _paymentService.CreateOrUpdatePaymentIntent(basket);

            if (intent == null)
                return BadRequest(new ProblemDetails { Title = "Problem creating payment intent" });

            // Update basket with payment intent details
            basket.PaymentIntentId = basket.PaymentIntentId ?? intent.Id;
            basket.ClientSecret = basket.ClientSecret ?? intent.ClientSecret;
            _context.Update(basket);

            // Save changes to database
            var result = await _context.SaveChangesAsync() > 0;

            if (!result)
                return BadRequest(new ProblemDetails { Title = "Problem updating basket with intent" });

            // Map basket to DTO and return
            return basket.MapBasketToDto();
        }

        // // Webhook endpoint for Stripe events
        // public async Task<ActionResult> StripeWebhook()
        // {
        //     // Read the request body as JSON
        //     var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        //     // Construct the event from the received JSON and validate the signature
        //     var stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"],
        //         _config["StripeSettings:WhSecret"]);

        //     // Extract the charge object from the event
        //     var charge = (Charge)stripeEvent.Data.Object;

        //     // Check charge status against enum value
        //     var status = Enum.Parse<ChargeStatus>(charge.Status, ignoreCase: true);

        //     // Find the order associated with the payment intent
        //     var order = await _context.Orders.FirstOrDefaultAsync(x =>
        //         x.PaymentIntentId == charge.PaymentIntentId);

        //     // Update order status based on charge status
        //     switch (status)
        //     {
        //         case ChargeStatus.Succeeded:
        //             order.OrderStatus = OrderStatus.PaymentReceived;
        //             break;
        //         // will add other status. . . 
        //         default:
        //             break;
        //     }

        //     // Save changes to database
        //     await _context.SaveChangesAsync();

        //     // Return an empty result to indicate successful processing of the webhook
        //     return new EmptyResult();
        // }
    }
}
