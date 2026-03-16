using ApiRRHH.Services;

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();
builder.Services.AddScoped<PayrollService>();
builder.Services.AddScoped<WhatsappService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PayrollServicePage>();

var app = builder.Build();

// Pipeline HTTP
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/", () => "API RRHH funcionando");

app.Run();