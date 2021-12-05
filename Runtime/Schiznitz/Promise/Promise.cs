using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Schiznitz.Promise
{
    public class PromiseBuilder
    {
        Promise _promise;
        public static PromiseBuilder Create() => new PromiseBuilder();
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
        public void SetResult()
        {
            Task.Resolve();
        }
        public void SetException(Exception exception)
        {
        }
        public Promise Task => _promise ??= new Promise();
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }
    }
    public class PromiseAwaiter : INotifyCompletion
    {
        public Action _continuation;
        public bool IsCompleted { get; private set; }
        public void Complete()
        {
            if (IsCompleted)
                return;
            IsCompleted = true;
            _continuation?.Invoke();
        }
        public void GetResult()
        {
        }
        public void OnCompleted(Action continuation)
        {
            if (IsCompleted)
                continuation();
            else
                _continuation = continuation;
        }
    }
    [AsyncMethodBuilder(typeof(PromiseBuilder))]
    public class Promise
    {
        public class Scheduler : MonoBehaviour { };
        public PromiseAwaiter _awaiter = new PromiseAwaiter();
        public void Resolve()
        {
            _awaiter.Complete();
        }
        public PromiseAwaiter GetAwaiter()
        {
            return _awaiter;
        }
        public static Lazy<Scheduler> _scheduler = new Lazy<Scheduler>(() =>
        {
            return new GameObject("_PromiseScheduler").AddComponent<Scheduler>();
        });
        public static IEnumerator _Run(object thing, Promise promise)
        {
            yield return thing;
            promise.Resolve();
        }
        public static IEnumerator _Run(object thing, Promise<object> promise)
        {
            yield return thing;
            promise.Resolve(null);
        }
        public static Promise StartCoroutine(IEnumerator routine)
        {
            var promise = new Promise();
            _scheduler.Value.StartCoroutine(_Run(routine, promise));
            return promise;
        }
        public static Promise Yield(YieldInstruction instruction)
        {
            var promise = new Promise();
            _scheduler.Value.StartCoroutine(_Run(instruction, promise));
            return promise;
        }
        public static Promise WaitForSeconds(float seconds)
        {
            return Yield(new WaitForSeconds(seconds));
        }
        public static Promise WaitUntil(Func<bool> predicate)
        {
            return StartCoroutine(new WaitUntil(predicate));
        }
        public static Promise All(params Promise[] promises)
        {
            var complete_all = new Promise();
            int num = promises.Length;
            foreach (var promise in promises)
            {
                Action wait = async () =>
                {
                    await promise;
                    num--;
                    if (num == 0)
                    {
                        complete_all.Resolve();
                    }
                };
                wait();
            }
            return complete_all;
        }
        public static Promise Race(params Promise[] promises)
        {
            var complete_one = new Promise();
            var any_completed = false;
            foreach (var promise in promises)
            {
                Action wait = async () =>
                {
                    await promise;
                    if (any_completed)
                        return;
                    any_completed = true;
                    complete_one.Resolve();
                };
                wait();
            }
            return complete_one;
        }
    }
    public class PromiseBuilder<T>
    {
        public Promise<T> _promise;
        public static PromiseBuilder<T> Create() => new PromiseBuilder<T>();
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
        public void SetResult(T value)
        {
            Task.Resolve(value);
        }
        public void SetException(Exception exception)
        {
        }
        public Promise<T> Task => _promise ??= new Promise<T>();
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
        }
    }
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
    [AsyncMethodBuilder(typeof(PromiseBuilder<>))]
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
        public static Promise<T[]> All(params Promise<T>[] promises)
        {
            var results = new T[promises.Length];
            var complete_all = new Promise<T[]>();
            int num = promises.Length;
            for (int i = 0; i < promises.Length; i++)
            {
                var promise = promises[i];
                Action wait = async () =>
                {
                    results[i] = await promise;
                    num--;
                    if (num == 0)
                    {
                        complete_all.Resolve(results);
                    }
                };
                wait();
            }
            return complete_all;
        }
        public static Promise<T> Race(params Promise<T>[] promises)
        {
            var complete_one = new Promise<T>();
            var any_completed = false;
            foreach (var promise in promises)
            {
                Action wait = async () =>
                {
                    var result = await promise;
                    if (any_completed)
                        return;
                    any_completed = true;
                    complete_one.Resolve(result);
                };
                wait();
            }
            return complete_one;
        }
    }
}
