using ApiRRHH.Services;

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
builder.Services.AddScoped<PayrollService>();
builder.Services.AddScoped<WhatsappService>();
builder.Services.AddScoped<PayrollServicePage>();
builder.Services.AddScoped<PortalAuthService>();

builder.Services.AddSingleton<BitacoraService>(sp =>
    new BitacoraService(
        sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!
    )
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirWeb", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Pipeline HTTP
app.UseCors("PermitirWeb");
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/", () => "API RRHH funcionando");

app.Run();