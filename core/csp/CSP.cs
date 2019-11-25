/** Scheduler.cs
 *  Author:         bagaking <kinghand@foxmail.com>
 *  CreateTime:     2019/10/08 17:43:51
 *  Copyright:      (C) 2019 - 2029 bagaking, All Rights Reserved
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniKh.extensions;
using System;
using UniKh.utils;

namespace UniKh.core {
    using csp;

    public class CSP : Singleton<CSP> {

        public static List<Proc> procToRun = new List<Proc>(); // todo: load balancing, priority;
        
        public Proc Do(IEnumerator _procedure, string tag = "_", Func<bool> validator = null) {
            if (!sw.IsRunning) {
                sw.Start(); // todo
            }

            var proc = new Proc(_procedure, tag, validator);
            procToRun.QueuePush(proc);
            return proc;
        }

        internal event Action ticks;
        internal System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        public event Action<string> PrintInfo = null;


        public List<Proc> procLst = new List<Proc>();

        public long TotalTicks { get; private set; } = 0;
        public int LastTickIndex { get; private set; } = 0;

        public long MonitExecutedInFrame { get; private set; } = 0;
        public long MonitTickTimeCost { get; private set; } = 0;

        public long MonitTotalUpdates { get; private set; } = 0;

        public void TriggerTick() {

            while (procToRun.Count > 0) {
                Reg(procToRun.QueuePop().Start());
            }
            
            
            MonitExecutedInFrame = 0;

            if (procLst.Count <= 0) return;

            TotalTicks += 1;

            int maxTickDurationMS = CONST.TIME_SPAN_MS_MAX;
            int maxExecuteRound = procLst.Count * CONST.MAX_ROUND;

            long startTimeMS = sw.ElapsedMilliseconds;

            int startIndex = (LastTickIndex % procLst.Count);

            int roundOfTick = 0;

            while (sw.ElapsedMilliseconds - startTimeMS <= maxTickDurationMS && procLst.Count > 0) {
                if (roundOfTick >= maxExecuteRound) { //������������Ѿ�����MAX_ROUND, �����ȴ���
                    break;
                }

                LastTickIndex %= procLst.Count;

                Proc currentProc = procLst[LastTickIndex];
                if (!currentProc.isActive) { // ����ʧЧ proc
                    Rem(currentProc);
                    continue;
                }

                if (currentProc.Validator != null) {
                    var result = currentProc.Validator();
                    if (!result) {
                        Rem(currentProc);
                        continue;
                    }
                }

                currentProc.Tick(maxTickDurationMS / procLst.Count); // ����ʱ�䴰��, �����п��ܱ仯 (���� REM �Ļ�)
                MonitExecutedInFrame += 1;

                LastTickIndex++; //ÿ������
                roundOfTick++; //��������


                if (startIndex == LastTickIndex % procLst.Count) { // ����Ѿ�ѭ��һ����, ������СTimeSpan����. ����ɾ������ܵ�����һ��������Զ�޷�����, ����߽粻�迼��
                    maxTickDurationMS = CONST.TIME_SPAN_MS_MIN;
                }

            }
            MonitTickTimeCost = sw.ElapsedMilliseconds - startTimeMS;
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
            MonitTotalUpdates += 1;
            if (0 == MonitTotalUpdates % 600) { 
                CheckHealth(true); // todo: to add a switch in UniKh/Const
            }
            
            TriggerTick();
        }

        public bool CheckHealth(bool printHealth = true) {
            if (printHealth) {
                var sb = SGen.New["CSP Status:"]["ticks-"][TotalTicks][",procs-"][procLst.Count];
                procLst.ForEach(p => sb['#'][p.Tag]['.'][null != p.GetOpCurr() ? p.GetOpCurr().ToString() : "null"].Append(","));
                Debug.Log(sb.End);
            }
            
            return true; // there no unhealthy situation for now 
        }
    }
}

