using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace PingMonitor
{
    internal static class PingProbe
    {
        public static async Task<PingMeasurement> MeasureAsync(PingTarget target, SemaphoreSlim gate, CancellationToken token)
        {
            bool entered = false;
            try
            {
                await gate.WaitAsync(token).ConfigureAwait(false);
                entered = true;
                token.ThrowIfCancellationRequested();
                // Framework Ping captures the caller's synchronization context. Start it
                // on a worker so neither DNS setup nor completion depends on the UI pump.
                return await Task.Run(() => SendAsync(target, token)).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                token.ThrowIfCancellationRequested();
                return new PingMeasurement { ServerId = target.ServerId, IsMeasurementError = true, Error = ex.GetBaseException().Message };
            }
            finally { if (entered) gate.Release(); }
        }

        private static async Task<PingMeasurement> SendAsync(PingTarget target, CancellationToken token)
        {
            using (Ping ping = new Ping())
            {
                token.ThrowIfCancellationRequested();
                Task<PingReply> request = ping.SendPingAsync(target.Host, 2000);
                Task cancellationWork = Task.CompletedTask;
                CancellationTokenRegistration registration = token.Register(() =>
                {
                    // SendAsyncCancel waits for the in-flight ICMP request. Never run
                    // that wait inside CancellationTokenSource.Cancel on the UI thread.
                    cancellationWork = Task.Run(() =>
                    {
                        try { ping.SendAsyncCancel(); }
                        catch (InvalidOperationException) { }
                    });
                });
                try
                {
                    PingReply reply = await request.ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    return new PingMeasurement
                    {
                        ServerId = target.ServerId,
                        IsSuccess = reply.Status == IPStatus.Success,
                        RoundtripTime = reply.Status == IPStatus.Success ? reply.RoundtripTime : 0,
                        Error = reply.Status.ToString()
                    };
                }
                finally
                {
                    // Registration disposal joins a running callback before reading its
                    // task. Keep Ping and the semaphore slot alive until cancellation drains.
                    registration.Dispose();
                    await cancellationWork.ConfigureAwait(false);
                }
            }
        }
    }
}
