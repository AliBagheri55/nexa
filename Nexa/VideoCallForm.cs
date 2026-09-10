using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Web.WebView2.Core;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexa
{
    public partial class VideoCallForm : Form
    {
        private int currentUserId;
        private int otherUserId;
        private bool isCaller;
        private bool bothReady;
        private bool offerCreated;
        private bool readySent;
        private bool callEnded;
        private HubConnection callConnection;

        public VideoCallForm(
            int currentUserId,
            int otherUserId,
            bool isCaller)
        {
            InitializeComponent();

            this.currentUserId = currentUserId;
            this.otherUserId = otherUserId;
            this.isCaller = isCaller;
        }

        private async void VideoCallForm_Load(
            object sender,
            EventArgs e)
        {
            try
            {
                await webView21.EnsureCoreWebView2Async();

                webView21.CoreWebView2.WebMessageReceived +=
                    CoreWebView2_WebMessageReceived;

                webView21.CoreWebView2.NavigationCompleted +=
                    WebView21_NavigationCompleted;

                string htmlPath =
                    System.IO.Path.Combine(
                        Application.StartupPath,
                        "WebRTC",
                        "call.html");

                webView21.Source = new Uri(htmlPath);

                await ConnectToCallServer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطا در VideoCallForm:\n\n" + ex.Message,
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async Task ConnectToCallServer()
        {
            callConnection =
                new HubConnectionBuilder()
                    .WithUrl("http://localhost:5291/callHub")
                    .WithAutomaticReconnect()
                    .Build();

            callConnection.On(
                "BothReady",
                async () =>
                {
                    bothReady = true;

                    if (!isCaller)
                        return;

                    if (offerCreated)
                        return;

                    offerCreated = true;

                    try
                    {
                        await InvokeOnUIAsync(
                            async () =>
                            {
                                await ExecuteJavaScript(
                                    "createOffer");
                            });
                    }
                    catch (Exception ex)
                    {
                        offerCreated = false;

                        BeginInvoke(
                            new Action(
                                () =>
                                {
                                    MessageBox.Show(
                                        "خطا در ایجاد Offer:\n\n" +
                                        ex.Message,
                                        "Nexa",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                                }));
                    }
                });

            callConnection.On<string, string>
    (
    "ReceiveOffer",
    async (senderId, offer) =>
    {
        try
        {
            int senderIdInt;

            if (int.TryParse(
            senderId,
            out senderIdInt))
            {
                if (senderIdInt != otherUserId)
                    return;
            }

            await InvokeOnUIAsync(
            async () =>
            {
                await ExecuteJavaScript(
        "receiveOffer",
        offer);
            });
        }
        catch (Exception ex)
        {
            BeginInvoke(
            new Action(
            () =>
            {
                MessageBox.Show(
        "خطا در دریافت Offer:\n\n" +
        ex.Message,
        "Nexa",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error);
            }));
        }
    });

            callConnection.On<string, string>
                (
                "ReceiveAnswer",
                async (senderId, answer) =>
                {
                    try
                    {
                        int senderIdInt;

                        if (int.TryParse(
                senderId,
                out senderIdInt))
                        {
                            if (senderIdInt != otherUserId)
                                return;
                        }

                        await InvokeOnUIAsync(
                async () =>
                {
                    await ExecuteJavaScript(
            "receiveAnswer",
            answer);
                });
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke(
                new Action(
                () =>
                {
                    MessageBox.Show(
            "خطا در دریافت Answer:\n\n" +
            ex.Message,
            "Nexa",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
                }));
                    }
                });

            callConnection.On<string, string>
                (
                "ReceiveIceCandidate",
                async (senderId, candidate) =>
                {
                    try
                    {
                        int senderIdInt;

                        if (int.TryParse(
                    senderId,
                    out senderIdInt))
                        {
                            if (senderIdInt != otherUserId)
                                return;
                        }

                        await InvokeOnUIAsync(
                    async () =>
                {
                    await ExecuteJavaScript(
                "receiveIceCandidate",
                candidate);
                });
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke(
                    new Action(
                    () =>
                {
                    MessageBox.Show(
                "خطا در دریافت ICE Candidate:\n\n" +
                ex.Message,
                "Nexa",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
                }));
                    }
                });

            callConnection.On<string>
                (
                "CallEnded",
                senderId =>
                {
                    BeginInvoke(
                    new Action(
                    () =>
                    {
                        if (callEnded)
                            return;

                        callEnded = true;

                        MessageBox.Show(
                    "تماس توسط طرف مقابل پایان یافت.",
                    "Nexa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                        Close();
                    }));
                });

            await callConnection.StartAsync();

            await callConnection.InvokeAsync(
            "RegisterUser",
            currentUserId.ToString());
        }

        private async void WebView21_NavigationCompleted(
        object sender,
        CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
                return;

            try
            {
                await InvokeOnUIAsync(
                async () =>
                {
                    await ExecuteJavaScript(
                    "startCamera");
                });

                if (!readySent &&
                callConnection != null &&
                callConnection.State ==
                HubConnectionState.Connected)
                {
                    readySent = true;

                    await callConnection.InvokeAsync(
                    "CallReady",
                    currentUserId.ToString(),
                    otherUserId.ToString());
                }
            }
            catch (Exception ex)
            {
                readySent = false;

                MessageBox.Show(
                "خطا در آماده‌سازی تماس:\n\n" +
                ex.Message,
                "Nexa",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            }
        }

        private async void CoreWebView2_WebMessageReceived(
        object sender,
        CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string message = e.WebMessageAsJson;

                using (JsonDocument document =
                JsonDocument.Parse(message))
                {
                    JsonElement root =
                    document.RootElement;

                    JsonElement typeElement;

                    if (!root.TryGetProperty(
                    "type",
                    out typeElement))
                        return;

                    string type =
                    typeElement.GetString();

                    if (type == "offer")
                    {
                        JsonElement offerElement;

                        if (!root.TryGetProperty(
                        "offer",
                        out offerElement))
                            return;

                        string offer =
                        offerElement.GetString();

                        if (string.IsNullOrEmpty(offer))
                            return;

                        await callConnection.InvokeAsync(
                        "SendOffer",
                        currentUserId.ToString(),
                        otherUserId.ToString(),
                        offer);
                    }
                    else if (type == "answer")
                    {
                        JsonElement answerElement;

                        if (!root.TryGetProperty(
                        "answer",
                        out answerElement))
                            return;

                        string answer =
                        answerElement.GetString();

                        if (string.IsNullOrEmpty(answer))
                            return;

                        await callConnection.InvokeAsync(
                        "SendAnswer",
                        currentUserId.ToString(),
                        otherUserId.ToString(),
                        answer);
                    }
                    else if (type == "iceCandidate")
                    {
                        JsonElement candidateElement;

                        if (!root.TryGetProperty(
                        "candidate",
                        out candidateElement))
                            return;

                        string candidate =
                        candidateElement.GetString();

                        if (string.IsNullOrEmpty(candidate))
                            return;

                        await callConnection.InvokeAsync(
                        "SendIceCandidate",
                        currentUserId.ToString(),
                        otherUserId.ToString(),
                        candidate);
                    }
                    else if (type == "endCall")
                    {
                        await EndCall();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                "خطا در WebRTC:\n\n" +
                ex.Message,
                "Nexa",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            }
        }

        private async Task ExecuteJavaScript(
        string functionName,
        string parameter = null)
        {
            if (webView21.CoreWebView2 == null)
                return;

            if (parameter == null)
            {
                await webView21.CoreWebView2
                .ExecuteScriptAsync(
                functionName + "();");
            }
            else
            {
                string jsonParameter =
                JsonSerializer.Serialize(parameter);

                await webView21.CoreWebView2
                .ExecuteScriptAsync(
                functionName +
                "(" +
                jsonParameter +
                ");");
            }
        }

        private Task InvokeOnUIAsync(
        Func<Task>
            action)
        {
            TaskCompletionSource<bool>
                tcs =
                new TaskCompletionSource<bool>
                    ();

            if (InvokeRequired)
            {
                BeginInvoke(
                new Action(
                async () =>
                {
                    try
                    {
                        await action();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }));
            }
            else
            {
                try
                {
                    Task task = action();

                    task.ContinueWith(
                    t =>
                    {
                        if (t.IsFaulted)
                            tcs.SetException(t.Exception);
                        else if (t.IsCanceled)
                            tcs.SetCanceled();
                        else
                            tcs.SetResult(true);
                    });
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }

            return tcs.Task;
        }

        private async Task EndCall()
        {
            if (callEnded)
                return;

            callEnded = true;

            try
            {
                if (callConnection != null &&
                callConnection.State ==
                HubConnectionState.Connected)
                {
                    await callConnection.InvokeAsync(
                    "EndCall",
                    currentUserId.ToString(),
                    otherUserId.ToString());
                }
            }
            catch
            {
            }

            try
            {
                if (webView21.CoreWebView2 != null)
                {
                    await ExecuteJavaScript(
                    "endCall");
                }
            }
            catch
            {
            }

            Close();
        }

        protected override async void OnFormClosing(
        FormClosingEventArgs e)
        {
            try
            {
                if (callConnection != null)
                {
                    if (callConnection.State !=
                    HubConnectionState.Disconnected)
                    {
                        await callConnection.StopAsync();
                    }

                    await callConnection.DisposeAsync();
                }
            }
            catch
            {
            }

            base.OnFormClosing(e);
        }
    }
}
