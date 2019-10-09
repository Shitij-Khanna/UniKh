/** Scheduler.cs
 *  Author:         bagaking <kinghand@foxmail.com>
 *  CreateTime:     2019/10/08 17:43:51
 *  Copyright:      (C) 2019 - 2029 bagaking, All Rights Reserved
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UniKh.core;
using UniKh.extensions;
using System;

namespace UniKh.core {
    using csp;

    public class CSP : Singleton<CSP> {

        public Proc Do(IEnumerator _procedure, string tag = "_") {
            if (!sw.IsRunning) {
                sw.Start(); // todo
            }

            return Reg(new Proc(_procedure, tag).Start());
        }

        internal event Action ticks;
        internal System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        public event Action<string> PrintInfo = null;


        public List<Proc> procLst = new List<Proc>();

        public long total_system_frame { get; private set; }
        //public static long count_triggered_corou_by_frame { get; private set; }

        private long last_tick_time_ms = 0;
        private int last_tick_index = 0;

        public void TriggerTick() {
            if (procLst.Count <= 0) return;

            total_system_frame += 1;

            int maxTickDurationMS = CONST.TIME_SPAN_MS_MAX;
            int maxExecuteRound = procLst.Count * CONST.MAX_ROUND;

            long startTimeMS = sw.ElapsedMilliseconds;

            int startIndex = (last_tick_index % procLst.Count);

            int roundOfTick = 0;

            while (sw.ElapsedMilliseconds - startTimeMS <= maxTickDurationMS && procLst.Count > 0) {
                if (roundOfTick >= maxExecuteRound) { //������������Ѿ�����MAX_ROUND, �����ȴ���
                    break;
                }

                last_tick_index %= procLst.Count;

                Proc currentProc = procLst[last_tick_index];
                if (!currentProc.isActive) { // ����ʧЧ proc
                    Rem(currentProc);
                    continue;
                }

                currentProc.Tick(maxTickDurationMS / procLst.Count); // ����ʱ�䴰��, �����п��ܱ仯 (���� REM �Ļ�)

                last_tick_index++; //ÿ������
                roundOfTick++; //��������


                if (startIndex == last_tick_index % procLst.Count) { // ����Ѿ�ѭ��һ����, ������СTimeSpan����. ����ɾ������ܵ�����һ��������Զ�޷�����, ����߽粻�迼��
                    maxTickDurationMS = CONST.TIME_SPAN_MS_MIN;
                }

            }
            last_tick_time_ms = sw.ElapsedMilliseconds;
        }

        public Proc Reg(Proc proc) {
            procLst.Append(proc);
            return proc;
        }
        public Proc Rem(Proc proc) {
            procLst.Remove(proc);
            return proc;
        }

        public void AddTick(Action t) { ticks += t; }
        public void RemTick(Action t) { ticks -= t; }

        public void Update() {
            TriggerTick();
        }
    }
}

