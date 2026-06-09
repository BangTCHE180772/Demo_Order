using Demo_Order.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// SignalR Hub
app.MapHub<OrderHub>("/orderHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Table}/{action=FloorPlan}/{id?}");

app.Run();
