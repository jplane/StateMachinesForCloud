using SM4C.Model.Actions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SM4C.Engine.Extensions
{
    internal static class RetryExtensions
    {
        public static async Task<bool> ShouldRetryAsync(this RetryPolicy retryPolicy,
                                                        StateMachineContext context,
                                                        int attempts,
                                                        TimeSpan elapsedDelay)
        {
            context.CheckArgNull(nameof(context));

            if (retryPolicy == null)
            {
                return false;
            }
            else if (attempts >= retryPolicy.MaxAttempts)
            {
                return false;
            }
            else if (retryPolicy.Delay != null)
            {
                var delay = attempts == 1 ? retryPolicy.Delay.Value : elapsedDelay;

                if (retryPolicy.Increment != null)
                {
                    delay += retryPolicy.Increment.Value;
                }
                else if (retryPolicy.Multiplier != null)
                {
                    delay *= retryPolicy.Multiplier.Value;
                }

                static TimeSpan applyJitter(TimeSpan ts, double factor, StateMachineContext ctxt)
                {
                    var increment = ctxt.Host.GetRandomBool();

                    var jitter = TimeSpan.FromSeconds(ctxt.Host.GetRandomDouble() * factor);

                    if (increment)
                        ts += jitter;
                    else
                        ts -= jitter;

                    return ts;
                }

                if (retryPolicy.Jitter != null)
                {
                    var factor = elapsedDelay.TotalSeconds * retryPolicy.Jitter.Value;
                    delay = applyJitter(delay, factor, context);
                }
                else if (retryPolicy.JitterTimeSpan != null)
                {
                    var factor = retryPolicy.JitterTimeSpan.Value.TotalSeconds;
                    delay = applyJitter(delay, factor, context);
                }

                if (retryPolicy.MaxDelay != null)
                {
                    delay = retryPolicy.MaxDelay.Value < delay ? retryPolicy.MaxDelay.Value : delay;
                }

                Debug.Assert(context.Host != null);

                await context.Host.DelayAsync(delay, context.CancelToken);
            }

            return true;
        }
    }
}
