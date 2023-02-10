using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using ODataBookStore.Models;
using ODataBookStore.DataAccess;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.AspNetCore.Http;

namespace ODataBookStore
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
            services.AddDbContext<BookStoreContext>(options =>
                options.UseInMemoryDatabase("InMemory")
            );
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ODataBookStore", Version = "v1" });
            });

            services.AddControllers().AddOData(o => o.Select().Filter()
            .Count().OrderBy().Expand().SetMaxTop(100)
            .AddRouteComponents("odata", GetEdmModel()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ODataBookStore v1"));
            }

            app.UseODataBatching();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.Use(next => context =>
            {
                var endpoints = context.GetEndpoint();
                if (endpoints == null)
                {
                    return next(context);
                }

                IEnumerable<string> templates;
                IODataRoutingMetadata metadata = endpoints.Metadata.GetMetadata<IODataRoutingMetadata>();

                if (metadata != null)
                {
                    templates = metadata.Template.GetTemplates();
                }

                return next(context);
            });

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapODataRoute("odata", "odata", GetEdmModel());
                endpoints.MapControllers();
            });
        }

        private static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Book>("Books");
            builder.EntitySet<Press>("Presses");
            return builder.GetEdmModel();
        }
    }
}
