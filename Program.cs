using Microsoft.EntityFrameworkCore;
using System.Text;
using TaskManagerAPI.Data;
using TaskManagerAPI.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

//���������� Serilog �� �������� ��������
var logFilePath = @"C:\Logs\log.txt";
Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()   //� ������� ���
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Debug)
    .WriteTo.File(
        logFilePath,
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: LogEventLevel.Debug) // � ���� ����� ������ Warning � ����
    .CreateLogger();

builder.Host.UseSerilog();

//controllers
builder.Services.AddControllers();

//��������� �����������
builder.Services.AddMemoryCache();

//swagger + jwt auth
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "������� ����� � �������: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

//��������� SQLite
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlite("Data Source=tasks.db"));

//���������� EF-�����������
builder.Services.AddScoped<ITaskRepository, EfTaskRepository>();

//automapper
builder.Services.AddAutoMapper(typeof(Program));

//jwt
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {        
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

//CORS ��������� 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()   //��������� ������� � ������ ������
            .AllowAnyMethod()   //��������� ��� HTTP-������ (GET, POST, PUT � ��)
            .AllowAnyHeader();  //��������� ��� ��������� (Content-Type, Authorization � ��)
    });
});

var app = builder.Build();

//swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//cors �� ��������������
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

//��� middleware
app.UseMiddleware<TaskManagerAPI.Middleware.ErrorHandlingMiddleware>();

app.MapControllers();

app.Run();