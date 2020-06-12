using Autofac;
using AutoMapper;
using Hangfire;
using Hangfire.MySql.Core;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Project.Core.Domain.Identity;
using Project.Infrastructure;
using Project.Infrastructure.EntityFrameworkCore;
using Project.Web.Appliccation.SignalrRHub;
using Project.Web.Infrastructure;
using Project.Web.Infrastructure.StartupExtensions;
using System.Text;

namespace Project.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        /// <summary>
        /// ����
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            //����http������
            services.AddHttpContextAccessor();
            //���.netcore ��������
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            IdentityModelEventSource.ShowPII = true;//��ʾ�������ϸ��Ϣ���鿴����
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
            //��������
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSameDomain",
                    policy => policy.SetIsOriginAllowed(origin => true)
                                    .AllowAnyMethod()
                                    .AllowAnyHeader()
                                    .AllowCredentials());
            });
            //����hangfire��ʱ����
            services.AddHangfire(x => x.UseStorage(new MySqlStorage(Configuration.GetConnectionString("MySql") + ";Allow User Variables=true", new MySqlStorageOptions
            {
                TablePrefix = "Hangfire"
            })));
            services.AddConfig(Configuration);
            services.AddApplicationDbContext(Configuration);//DbContext������
            services.AddIdentityOptions();
            services.AddAutoMapper(typeof(Startup));//autoap
            services.AddMediatR(typeof(Startup));//CQRS
            services.AddHealthChecks();//�������
            services.AddAuthService(Configuration);//��֤����
            services.AddSignalR();
            services.AddApiVersion();//api�汾
            services.AddController();//api������
            services.AddIdentity<ApplicationUser, ApplicationRole>()
                  .AddEntityFrameworkStores<ApplicationDbContext>()
                  .AddDefaultTokenProviders();//Identity ע��
        }
        /// <summary>
        /// �м���ܵ�
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseCors("AllowSameDomain");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseHealthChecks("/health");
            app.UseApiVersioning();
            app.UseLog4net();
            app.UseErrorHandling();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<ProjectHub>("/project").RequireCors(t => t.WithOrigins(new string[] { "null" }).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
            });
            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                WorkerCount = 1
            });
            RegisterJobs();
            //��ʼ������
            DbContextSeed.SeedAsync(app.ApplicationServices).Wait();
        }

        /// <summary>
        /// ��ʱ����
        /// </summary>
        private static void RegisterJobs()
        {
            //15
           // RecurringJob.AddOrUpdate<IHanfireTaskService>("OrderReceivedConfirmAuto", job => job.OrderReceivedConfirmAuto(15), Cron.Minutely);//"0 */1 * * * ?"
        }
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new DependencyRegistrar());
        }
    }

}