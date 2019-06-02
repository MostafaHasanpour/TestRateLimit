using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TestRateLimit
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            // needed to store rate limit counters and ip rules
            services.AddMemoryCache();

            //load general configuration from appsettings.json
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));

            //load ip rules from appsettings.json
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

            //services.Configure<IpRa

            // inject counter and rules stores
            services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();

            // Add framework services.
            services.AddMvc();

            // https://github.com/aspnet/Hosting/issues/793
            // the IHttpContextAccessor service is not registered by default.
            // the clientId/clientIp resolvers use it.
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // configuration (resolvers, counter key builders)
            //services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddSingleton<IRateLimitConfiguration, CustomRateLimitConfiguration>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseIpRateLimiting();

            app.UseMvc();
        }
    }
    public class CustomRateLimitConfiguration : RateLimitConfiguration
    {
        public CustomRateLimitConfiguration(IHttpContextAccessor httpContextAccessor, IOptions<IpRateLimitOptions> ipOptions, IOptions<ClientRateLimitOptions> clientOptions) : base(httpContextAccessor, ipOptions, clientOptions)
        {
        }

        protected override void RegisterResolvers()
        {
            base.RegisterResolvers();

            ClientResolvers.Add(new ClientQueryStringResolveContributor(HttpContextAccessor, ClientRateLimitOptions.ClientIdHeader));
        }
    }

    internal class ClientQueryStringResolveContributor : IClientResolveContributor
    {
        private IHttpContextAccessor httpContextAccessor;
        private string clientIdHeader;

        public ClientQueryStringResolveContributor(IHttpContextAccessor httpContextAccessor, string clientIdHeader)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.clientIdHeader = clientIdHeader;
        }

        public string ResolveClient()
        {
            return "salam "+clientIdHeader;
        }
    }

    public class ClientHeaderResolveContributor : IClientResolveContributor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _headerName;

        public ClientHeaderResolveContributor(
            IHttpContextAccessor httpContextAccessor,
            string headerName)
        {
            _httpContextAccessor = httpContextAccessor;
            _headerName = headerName;
        }
        public string ResolveClient()
        {
            var clientId = "anon";
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext.Request.Headers.TryGetValue(_headerName, out var values))
            {
                clientId = values.First();
            }

            return clientId;
        }
    }
}
