using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Schiznitz.Promise
{
    public class PromiseAwaiter<T> : INotifyCompletion
    {
        public T _result;
        public Action _continuation;
        public bool IsCompleted { get; private set; }
        public void Complete(T result)
        {
            if (IsCompleted)
                return;
            _result = result;
            IsCompleted = true;
            _continuation?.Invoke();
        }
        public T GetResult()
        {
            return _result;
        }
        public void OnCompleted(Action continuation)
        {
            if (IsCompleted)
                continuation();
            else
                _continuation = continuation;
        }
    }
    public class Promise<T>
    {
        public PromiseAwaiter<T> _awaiter = new PromiseAwaiter<T>();
        public void Resolve(T result)
        {
            _awaiter.Complete(result);
        }
        public PromiseAwaiter<T> GetAwaiter()
        {
            return _awaiter;
        }
    }
    public class Promise
    {
        public class Scheduler : MonoBehaviour { };
        public static Lazy<Scheduler> _scheduler = new Lazy<Scheduler>(() =>
        {
            return new GameObject().AddComponent<Scheduler>();
        });
        public static IEnumerator _Run(object thing, Promise<object> promise)
        {
            yield return thing;
            promise.Resolve(null);
        }
        public static Promise<object> StartCoroutine(IEnumerator routine)
        {
            var promise = new Promise<object>();
            _scheduler.Value.StartCoroutine(_Run(routine, promise));
            return promise;
        }
        public static Promise<object> Yield(YieldInstruction instruction)
        {
            var promise = new Promise<object>();
            _scheduler.Value.StartCoroutine(_Run(instruction, promise));
            return promise;
        }
        public static Promise<object> WaitForSeconds(float seconds)
        {
            return Yield(new WaitForSeconds(seconds));
        }
        public static Promise<object> WaitUntil(Func<bool> predicate)
        {
            return StartCoroutine(new WaitUntil(predicate));
        }
    }
}
