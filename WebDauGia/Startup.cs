using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebDauGia.Startup))]
namespace WebDauGia
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
