using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using System;

public class ThreadedDataRequester : MonoBehaviour {
    static ThreadedDataRequester instance;
    Queue<ThreadInfo> DataQueue = new Queue<ThreadInfo>();

    void Awake() {
        instance = FindAnyObjectByType<ThreadedDataRequester>();
    }

	public static void RequestData(Func<object> generateData, Action<object> callback) {
		ThreadStart threadStart = delegate {
			instance.DataThread (generateData, callback);
		};

		new Thread (threadStart).Start ();
	}

	void DataThread(Func<object> generateData, Action<object> callback) {
        object data = generateData();
		lock (DataQueue) {
			DataQueue.Enqueue(new ThreadInfo(callback, data));
		}
	}

	void Update() {
		if (DataQueue.Count > 0) {
			for (int i = 0; i < DataQueue.Count; i++) {
				ThreadInfo threadInfo = DataQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}
	}

    struct ThreadInfo {
		public readonly Action<object> callback;
		public readonly object parameter;

		public ThreadInfo (Action<object> callback, object parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}
	}
}
