using LinkSummary.Api.BLL.Abstract;
using LinkSummary.Api.BLL.Services;
using LinkSummary.Api.Configuration;
using System.Net;

namespace LinkSummary.Api.AppStart
{
    public class Startup
    {
        private WebApplicationBuilder _builder;

        public Startup(WebApplicationBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public void Initialize()
        {
            if (_builder.Environment.IsDevelopment())
            {
                _builder.Services.AddSwaggerGen();
            }
            else
            {
                //_builder.Services.ConfigureCors();
            }

            InitConfigs();
            ConfigureClientAPI();

            _builder.Services.AddControllers();
        }

        private void InitConfigs()
        {
            _builder.Services.Configure<AiClientConfig>(_builder.Configuration.GetSection(AiClientConfig.SectionName));
            _builder.Services.Configure<ProxyConfig>(_builder.Configuration.GetSection(ProxyConfig.SectionName));
        }
        private void ConfigureClientAPI()
        {                        
            _builder.Services.AddHttpClient<IAiClient, ChatGptAiClient>((serviceProvider, client) =>
            {
                var aiClientConfig = _builder.Configuration.GetSection(AiClientConfig.SectionName).Get<AiClientConfig>();

                client.BaseAddress = new Uri("https://api.openai.com/v1/chat/completions");
                client.Timeout = TimeSpan.FromSeconds(35); // Таймаут запроса
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {aiClientConfig.OpenAiApiKey}");
            })
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    var handler = new HttpClientHandler();

                    // Получаем настройки прокси из конфигурации
                    var proxyConfig = _builder.Configuration.GetSection("ProxySettings").Get<ProxyConfig>();

                    if (proxyConfig?.Enabled == true && !string.IsNullOrEmpty(proxyConfig.Ip))
                    {
                        var proxy = new WebProxy
                        {
                            Address = new Uri($"http://{proxyConfig.Ip}:{proxyConfig.Port}"),
                            BypassProxyOnLocal = false,
                            UseDefaultCredentials = false
                        };

                        // Если есть логин и пароль
                        if (!string.IsNullOrEmpty(proxyConfig.Login) && !string.IsNullOrEmpty(proxyConfig.Password))
                        {
                            proxy.Credentials = new NetworkCredential(proxyConfig.Login, proxyConfig.Password);
                        }

                        handler.Proxy = proxy;
                        handler.UseProxy = true;
                    }
                    return handler;
                });
        }
    }
}
