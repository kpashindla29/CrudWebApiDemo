using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;

namespace CrudWebApiDemo.Models
{
    public class LoginModel
    {
        public string Username { get; set;}

        public string Password { get; set;}
    }
}
