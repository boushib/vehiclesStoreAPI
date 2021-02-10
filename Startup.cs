using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using vehiclesStoreAPI.Repositories;
using vehiclesStoreAPI.DAO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace vehiclesStoreAPI
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
      // services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      //   .AddJwtBearer(options =>
      //   {
      //     options.Audience = Configuration["AzureAD:ResourceId"];
      //     options.Authority = $"{Configuration["AzureAD:InstanceId"]}{Configuration["AzureAD:TenantId"]}";
      //   });
      // Read settings
      var jwtConfig = Configuration.GetSection("JWTConfig").Get<JWTConfig>();
      var connectionString = Configuration["PostgresConnectionString"];
      // Register jwt as Singleton in Dependency Injection (DI) container 
      services.AddSingleton(jwtConfig);
      services.AddDbContext<VehiclesContext>(options => options.UseNpgsql(connectionString));
      services.AddDbContext<UsersContext>(options => options.UseNpgsql(connectionString));
      services.AddControllers();
      services.AddAuthentication(options =>
      {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
      })
        .AddJwtBearer(options =>
        {
          options.RequireHttpsMetadata = true;
          // Save JWT access token in the current HttpContext, 
          // so that we can retrieve it using the method 
          // await HttpContext.GetTokenAsync(“Bearer”, “access_token”) or something similar.
          // If we want to set the SaveToken to be false, then we can save the JWT access token in claims, 
          // and then retrieve its value using the method: User.FindFirst("access_token")?.Value.
          options.SaveToken = true;
          options.TokenValidationParameters = new TokenValidationParameters
          {
            ValidateIssuer = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtConfig.Secret)),
            ValidAudience = jwtConfig.Audience,
            ValidateAudience = true,
            ValidateLifetime = true,
            // I set ClockSkew value to be 1min
            // to give an allowance time for the token expiration validation.
            ClockSkew = TimeSpan.FromMinutes(1),
          };
        });
      services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
      // Dependency Injection
      services.AddScoped<IVehicleRepository, VehicleRepository>();
      services.AddScoped<IUsersRepository, UsersRepository>();
      services.AddScoped<IJWTAuthRepository, JWTAuthRepository>();
      //services.AddHostedService<JwtRefreshTokenCache>();
      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "vehiclesStoreAPI", Version = "v1" });
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "vehiclesStoreAPI v1"));
      }
      else
      {
        app.UseHttpsRedirection();
      }

      app.UseRouting();
      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }
  }
}
