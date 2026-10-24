using System;
using System.Collections.Generic;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace ScrewKing
{
    // One tween controls flight progress. No rotation, scaling, bounce or secondary offset.
    public sealed class SimpleScrewFlight : IDisposable
    {
        public const float Duration = .35f;
        sealed class Flight
        {
            public Vector2 Start;
            public float Progress;
            public MotionHandle Handle;
        }
        readonly ManualMotionDispatcher dispatcher = new ManualMotionDispatcher();
        readonly Dictionary<int, Flight> flights = new Dictionary<int, Flight>();

        public SimpleScrewFlight()
        {
            // Register the float runner before idle time accrues; its first tick must not consume
            // the entire time spent on the level before the player clicked.
            dispatcher.EnsureStorageCapacity<float, NoOptions, FloatMotionAdapter>(16);
        }
        public void Begin(int id, Vector2 start)
        {
            Forget(id);
            var flight = new Flight { Start = start };
            flights[id] = flight;
            flight.Handle = LMotion.Create(0f, 1f, Duration)
                .WithEase(Ease.InOutSine)
                .WithScheduler(dispatcher.Scheduler)
                .Bind(progress => flight.Progress = progress);
        }
        public void Tick(float delta) => dispatcher.Update(delta);
        public Vector2 Position(int id, Vector2 target) => flights.TryGetValue(id, out var flight)
            ? Vector2.Lerp(flight.Start, target, flight.Progress) : target;
        public void Forget(int id)
        {
            if (!flights.TryGetValue(id, out var flight)) return;
            if (flight.Handle.IsActive()) flight.Handle.Cancel();
            flights.Remove(id);
        }
        public void Reset() { dispatcher.Reset(); flights.Clear(); }
        public void Dispose() => Reset();
    }
}
