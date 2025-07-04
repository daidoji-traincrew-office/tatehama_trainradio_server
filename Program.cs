using TatehamaRadioServer.Hubs;

var builder = WebApplication.CreateBuilder(args);

// CORSポリシーを追加して、どのオリジンからでも接続できるようにする
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(origin => true) // すべてのオリジンを許可
              .AllowCredentials();
    });
});

// SignalRサービスをコンテナに追加
builder.Services.AddSignalR();

var app = builder.Build();

// CORSを有効にする
app.UseCors();

// HTTPリクエストをHTTPSにリダイレクトする (本番環境で推奨)
app.UseHttpsRedirection();

// SignalRハブのエンドポイントをマッピング
// これにより、クライアントは "https://your-server.com/radioHub" のようなアドレスに接続できる
app.MapHub<RadioHub>("/radioHub");

// テスト用のルートエンドポイント
app.MapGet("/", () => "Tatehama Radio Relay Server is running.");

app.Run();
