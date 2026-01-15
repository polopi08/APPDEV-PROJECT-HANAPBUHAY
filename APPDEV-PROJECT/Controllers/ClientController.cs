using Microsoft.AspNetCore.Mvc;
using APPDEV_PROJECT.Helpers;
using System.Collections.Generic;

namespace APPDEV_PROJECT.Controllers
{
    public class ClientController : Controller
    {
        [HttpGet]
        [Route("api/workers/nearby")]
        public IActionResult GetNearbyWorkers(double lat, double lng, string skill = "", string search = "")
        {
            try
            {
                var mockWorkers = new List<WorkerFilterHelper.WorkerWithDistance>
                {
                    new() { Id = 1, Name = "Kyle Bernido", Skill = "Carpenter", Lat = 14.605, Lng = 121.030 },
                    new() { Id = 2, Name = "Vivian Yambao", Skill = "Cook", Lat = 14.603, Lng = 121.029 },
                    new() { Id = 3, Name = "Viaani Ubalde", Skill = "Electrician", Lat = 14.606, Lng = 121.028 },
                    new() { Id = 4, Name = "Kyle Bernido", Skill = "Technician", Lat = 14.604, Lng = 121.031 },
                    new() { Id = 5, Name = "Giselle Valdez", Skill = "Plumber", Lat = 14.602, Lng = 121.027 },
                    new() { Id = 6, Name = "Sophia Cutue", Skill = "Plumber", Lat = 14.603, Lng = 121.028 }
                };

                var filteredWorkers = WorkerFilterHelper.GetNearbyWorkers(mockWorkers, lat, lng, skill, search);
                return Json(filteredWorkers);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

/* changes, this is for messaging*/
        [HttpGet]
        [Route("api/messages/conversations")]
        public IActionResult GetConversations(string search = "")
        {
            try
            {
                var conversations = string.IsNullOrEmpty(search) 
                    ? MessageHelper.GetConversations()
                    : MessageHelper.SearchConversations(search);
                return Json(conversations);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/messages/{conversationId}")]
        public IActionResult GetMessages(int conversationId)
        {
            try
            {
                var messages = MessageHelper.GetMessages(conversationId);
                return Json(messages);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/messages/send")]
        public IActionResult SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Content))
                    return BadRequest(new { error = "Message content cannot be empty" });

                var message = MessageHelper.SendMessage(request.ConversationId, request.Content);
                return Json(message);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        public class SendMessageRequest
        {
            public int ConversationId { get; set; }
            public string Content { get; set; }
        }

/* hanggang dito */

        public IActionResult SearchPage_C()
        {
            return View();
        }

        public IActionResult InfoPage_C()
        {
            return View();
        }

        public IActionResult ChatPage()
        {
            return View();
        }

        public IActionResult Messages()
        {
            return View("ChatPage");
        }

        public IActionResult Notifications()
        {
            return View("NotifPage");
        }

        public IActionResult Profile()
        {
            return View();
        }
    }
}