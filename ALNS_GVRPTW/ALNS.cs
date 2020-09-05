﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
/* 
  @Author：Created by 张平 from 东北大学
  @Email:1257524054@qq.com
  @Date：Created in 2020.0504
 */
namespace ALNS_GVRPTW
{
    public class ALNS : TWJudgement
    {
        private double coef;
        private double Weights1=0.3;
        private double Weights2=0.4;
        private double Weights3=0.3;
        private double w=0.35;
        private double score1=9;
        private double score2=13;
        private double a1;
        private double a2;
        private double Car_weight;
        private double ms;
        private double minCusneed;
        private double max_minminCusneed;
        private double minCusdistance;
        private double max_minCusdistance;
        private int iterations;
        private delegate List<int> RemoveDelegate(int q, ref List<Route> S);//委托机制
        public ALNS(List<List<double>>_Cij, List<List<double>> _Tij,  double capacity, double ServiceTime, List<Request> customerSet, double _coef,double _a1, double _a2, double _Car_weight, double _ms, double _minCusneed, double _max_minminCusneed, double _minCusdistance, double _max_minCusdistance, int _iterations) : base(_Cij, _Tij, capacity, ServiceTime, customerSet)
        {
            coef = _coef;   
            a1 = _a1;
            a2 = _a2;
            Car_weight = _Car_weight;
            ms = _ms;
            minCusneed = _minCusneed;
            max_minminCusneed = _max_minminCusneed;
            minCusdistance = _minCusdistance;
            max_minCusdistance = _max_minCusdistance;
            iterations = _iterations;
        }
        public List<Route> GetSolution(int q, List<Route> solution)
        {
            Upgrade(solution);//添加上每个route的碳排放
            List<int> RequestBank = new List<int>();
            List<double> s1 = new List<double>();//储存各删除启发式适值
            List<double> s2 = new List<double>();//储存各插入启发式适值
            int[] n1 = new int[] { 0, 0, 0, 0 };
            int[] n2 = new int[] { 0, 0, 0 };
            List<Route> bestSolution = new List<Route>();
            for (int i = 0; i < solution.Count; i++)
            {
                Route route = new Route();//给bestSolution申请新的堆空间，切断与solution的牵连
                route.ID = solution[i].ID;
                route.routeCapacity = solution[i].routeCapacity;
                route.routeCost = solution[i].routeCost;
                for (int j = 0; j < solution[i].route.Count; j++)
                {
                    route.route.Add(solution[i].route[j]);
                    route.visitTime.Add(solution[i].visitTime[j]);
                    route.waitTime.Add(solution[i].waitTime[j]);
                }
                bestSolution.Add(route);
            }
            List<Route> Solution = new List<Route>();
            for (int i = 0; i < solution.Count; i++)
            {
                Route route = new Route();
                route.ID = solution[i].ID;
                route.routeCapacity = solution[i].routeCapacity;
                route.routeCost = solution[i].routeCost;
                for (int j = 0; j < solution[i].route.Count; j++)
                {
                    route.route.Add(solution[i].route[j]);
                    route.visitTime.Add(solution[i].visitTime[j]);
                    route.waitTime.Add(solution[i].waitTime[j]);
                }
                Solution.Add(route);
            }
            double f1 = SolutionCost(solution);
            double best = f1;
            double T = (-w * f1) / Math.Log(0.5, Math.E);//计算温度的初始值
            double p1 = 0,p2 = 0,p3 = 0, q1 = 0, q2 = 0;
            double Fitness = 1;//适值初始值
            double minFitness = 0.0001 * Fitness;//适值下限
            for (int i = 0; i < 4; i++)
            {
                s1.Add(Fitness);
            }
            for (int i = 0; i < 3; i++)
            {
                s2.Add(Fitness);
            }
            List<RemoveDelegate> destroy = new List<RemoveDelegate>() { RandomRemove,DfRemove,  /*WorstRemove,*/ RelationRemove, ClusterRemove/*, WorstRouteRemove */};
            int a = 0, b = 0;
            //算法开始
            Random R = new Random();
            for (int t = 0; t < iterations; t++)
            {
                p1 = s1[0] / s1.Sum();
                p2 = (s1[0] + s1[1]) / s1.Sum();
                p3 = (s1[0] + s1[1] + s1[2]) / s1.Sum();
                q1 = s2[0] / s2.Sum();
                q2 = (s2[0] + s2[1]) / s2.Sum();
                //选择一个破坏方法  
                double r1 = R.NextDouble();
                if (r1 <= p1)
                {
                    a = 0;
                }
                else if (r1 <= p2)
                {
                    a = 1;
                }
                else if (r1 <= p3)
                {
                    a = 2;
                }
                else
                {
                    a = 3;
                }
                RequestBank = Destroy(q, ref Solution, destroy[a]);//破坏操作
                //选择一个修复方法
                bool success = true;
                r1 = R.NextDouble();
                if (r1 <= q1)
                {
                    b = 0;
                }
                else if (r1 <= q2)
                {
                    b = 1;
                }
                else
                {
                    b = 2;
                }
                Repair(RequestBank, ref Solution, b, ref success);//修复操作
                n1[a] += 1;//记录相关破坏方法使用的次数，备用
                if (success)//得到可行解
                {
                    for (int i = 0; i < s1.Count; i++)//适值蒸发
                    {
                        s1[i] *= 0.999;
                    }
                    for (int i = 0; i < s2.Count; i++)
                    {
                        s2[i] *= 0.999;
                    }
                    T *= 0.9999;//降温
                    double f2 = SolutionCost(Solution);
                    n2[b] += 1;//记录相关修复方法使用的次数，备用
                    if (f2 < f1)//优于当前解
                    {
                        if (f2 < best)//优于历史最优解
                        {
                            s1[a] = s1[a] + (best - f2) / best;
                            s2[b] = s1[b] + (best - f2) / best;
                            best = f2;
                            bestSolution.Clear();
                            for (int i = 0; i < Solution.Count; i++)
                            {
                                Route route = new Route();//给bestSolution申请新的堆空间，切断与solution的牵连
                                route.ID = Solution[i].ID;
                                route.routeCapacity = Solution[i].routeCapacity;
                                route.routeCost = Solution[i].routeCost;
                                for (int j = 0; j < Solution[i].route.Count; j++)
                                {
                                    route.route.Add(Solution[i].route[j]);
                                    route.visitTime.Add(Solution[i].visitTime[j]);
                                    route.waitTime.Add(Solution[i].waitTime[j]);
                                }
                                bestSolution.Add(route);
                            }
                        }
                        else
                        {
                            //更新相应启发式适值；                          
                            s1[a] = s1[a] + (f1 - f2) / f1 ;
                            s2[b] = s1[b] + (f1 - f2) / f1 ;
                        }

                        //更新solution
                        f1 = f2;
                        solution.Clear();
                        for (int i = 0; i < Solution.Count; i++)
                        {
                            Route route = new Route();//给bestSolution申请新的堆空间，切断与solution的牵连
                            route.ID = Solution[i].ID;
                            route.routeCapacity = Solution[i].routeCapacity;
                            route.routeCost = Solution[i].routeCost;
                            for (int j = 0; j < Solution[i].route.Count; j++)
                            {
                                route.route.Add(Solution[i].route[j]);
                                route.visitTime.Add(Solution[i].visitTime[j]);
                                route.waitTime.Add(Solution[i].waitTime[j]);
                            }
                            solution.Add(route);
                        }
                    }
                    else if (f2 > f1)//比当前解差，运用模拟退火的规则按一定概率去接受该解
                    {
                        r1 = R.NextDouble();
                        if (r1 >= Math.Exp((f1 - f2) / T))//不接受差解
                        {
                            Solution.Clear();
                            for (int i = 0; i < solution.Count; i++)
                            {
                                Route route = new Route();//申请新的堆空间，切断与solution的牵连
                                route.ID = solution[i].ID;
                                route.routeCapacity = solution[i].routeCapacity;
                                route.routeCost = solution[i].routeCost;
                                for (int j = 0; j < solution[i].route.Count; j++)
                                {
                                    route.route.Add(solution[i].route[j]);
                                    route.visitTime.Add(solution[i].visitTime[j]);
                                    route.waitTime.Add(solution[i].waitTime[j]);
                                }
                                Solution.Add(route);
                            }
                        }
                    }
                    for (int i = 0; i < s1.Count; i++)//对各适值设置一个最低值，保证所有启发式都有一定几率参与到搜索中
                    {
                        if (s1[i] < minFitness)
                        {
                            s1[i] = minFitness;
                        }
                    }
                    for (int i = 0; i < s2.Count; i++)
                    {
                        if (s2[i] < minFitness)
                        {
                            s2[i] = minFitness;
                        }
                    }
                }
                else//非法解
                {
                    Solution.Clear();
                    for (int i = 0; i < solution.Count; i++)
                    {
                        Route route = new Route();//申请新的堆空间，切断与solution的牵连
                        route.ID = solution[i].ID;
                        route.routeCapacity = solution[i].routeCapacity;
                        route.routeCost = solution[i].routeCost;
                        for (int j = 0; j < solution[i].route.Count; j++)
                        {
                            route.route.Add(solution[i].route[j]);
                            route.visitTime.Add(solution[i].visitTime[j]);
                            route.waitTime.Add(solution[i].waitTime[j]);
                        }
                        Solution.Add(route);
                    }
                }
            }
            return bestSolution;
        }
        //委托
        private List<int> Destroy(int q, ref List<Route> S, RemoveDelegate Remove)//让方法名的地址指向方法
        {
            List<int> RequestBank = new List<int>();
            RequestBank = Remove(q, ref S);
            return RequestBank;
        }
        public List<int> RandomRemove(int q, ref List<Route> _solution)
        {
            List<int> RequestBank = new List<int>();
            List<int> record = new List<int>();
            Random r = new Random();
            int x;
            int y;
            do
            {
                x = r.Next(0, _solution.Count - 1);
                if (_solution[x].route.Count != 1)
                {
                    if (!record.Contains(x))
                    {
                        record.Add(x);
                    }
                    y = r.Next(1, _solution[x].route.Count - 1);
                    RequestBank.Add(_solution[x].route[y]);
                    _solution[x].visitTime.RemoveAt(y);
                    _solution[x].waitTime.RemoveAt(y);
                    _solution[x].route.RemoveAt(y);
                }
            } while (RequestBank.Count < q);
            RouteRemoveChange(_solution, record);
            return RequestBank;
        }
        public List<int> ClusterRemove(int q, ref List<Route> _solution)
        {
            List<int> RequestBank = new List<int>();
            List<int> record = new List<int>();
            Random r = new Random();
            int x;
            int y;
            do
            {
                x = r.Next(0, _solution.Count - 1);
                if (_solution[x].route.Count > 2)
                {
                    if (!record.Contains(x))
                    {
                        record.Add(x);
                    }
                    Route route = _solution[x];
                    List<int> requests = KruskalClassification(route);
                    for (int i = 0; i < requests.Count; i++)
                    {
                        RequestBank.Add(requests[i]);
                    }
                    _solution.RemoveAt(x);
                    _solution.Insert(x, route);
                }
            } while (RequestBank.Count < q);
            RouteRemoveChange(_solution, record);
            return RequestBank;
        }
        public List<int> DfRemove(int q, ref List<Route> _solution)
        {
            List<Tab> Change = new List<Tab>();
            List<int> RequestBank = new List<int>();
            List<int> record = new List<int>();
            for (int i = 0; i < _solution.Count; i++)
            {
                if (_solution[i].route.Count > 1)
                {
                    int j;
                    for (j = 1; j < _solution[i].route.Count - 1; j++)//route从0开始
                    {
                        Tab change = new Tab();
                        change.valueChange = CustomerSet[_solution[i].route[j]].cusneed * Cij[j][j + 1];
                        change.index.Add(i);
                        change.index.Add(j);
                        Change.Add(change);
                    }
                    Tab _change = new Tab();
                    _change.valueChange = CustomerSet[_solution[i].route[j]].cusneed * Cij[j][0];
                    _change.index.Add(i);
                    _change.index.Add(j);
                    Change.Add(_change);
                }
            }
            //这里采用选择排序法，在编写过程中，由于没考虑自身为当前最大的特殊情况，Debug很久，特意备注下。
            //排count个就行了
            int count = (int)(Math.Pow(0.9, 9) * (Change.Count)) + 1;
            for (int i = 0; i < count; i++)
            {
                int Row = i;
                double max = Change[i].valueChange;
                for (int j = i + 1; j < Change.Count; j++)
                {
                    if (max < Change[j].valueChange)
                    {
                        max = Change[j].valueChange;
                        Row = j;
                    }
                }
                if (Row != i)//此处把自身不为当前最大的元素进行交换
                {
                    //交换valueChange
                    double swap = Change[Row].valueChange;
                    Change[Row].valueChange = Change[i].valueChange;
                    Change[i].valueChange = swap;
                    //交换Index
                    List<int> s = new List<int>();
                    for (int t = 0; t < 2; t++)
                    {
                        s.Add(Change[Row].index[t]);
                    }
                    Change[Row].index[0] = Change[i].index[0];
                    Change[Row].index[1] = Change[i].index[1];
                    Change[i].index[0] = s[0];
                    Change[i].index[1] = s[1];
                }
            }
            for (int i = 0; i < q; i++)
            {
                int x = Change[i].index[0];
                int y = Change[i].index[1];//y不能为0
                RequestBank.Add(_solution[x].route[y]);
                if (!record.Contains(x))
                {
                    record.Add(x);
                }
            }
            for (int i = 0; i < record.Count; i++)//从_solution删除requestBank中的节点
            {
                int r = record[i];
                for (int j = 0; j < RequestBank.Count; j++)
                {
                    if (_solution[r].route.Contains(RequestBank[j]))
                    {
                        int t = _solution[r].route.IndexOf(RequestBank[j]);
                        _solution[r].route.RemoveAt(t);
                        _solution[r].visitTime.RemoveAt(t);
                        _solution[r].waitTime.RemoveAt(t);
                    }
                }
            }
            RouteRemoveChange(_solution, record);
            return RequestBank;
        }
        public List<int> WorstRemove(int q, ref List<Route> _solution)
        {
            List<Tab> Change = new List<Tab>();
            List<int> RequestBank = new List<int>();
            List<int> record = new List<int>();
            for (int i = 0; i < _solution.Count; i++)
            {
                for (int j = 1; j < _solution[i].route.Count; j++)//route从0开始
                {
                    Tab change = new Tab();
                    change.valueChange = WorstRemoveChange(_solution[i], j);
                    change.index.Add(i);
                    change.index.Add(j);
                    Change.Add(change);
                }
            }
            //这里采用选择排序法，在编写过程中，由于没考虑自身为当前最大的特殊情况，Debug很久，特意备注下。
            //排count个就行了
            int count = (int)(Math.Pow(0.9, 9) * (Change.Count)) + 1;
            for (int i = 0; i < count; i++)
            {
                int Row = i;
                double max = Change[i].valueChange;
                for (int j = i + 1; j < Change.Count; j++)
                {
                    if (max < Change[j].valueChange)
                    {
                        max = Change[j].valueChange;
                        Row = j;
                    }
                }
                if (Row != i)//此处把自身不为当前最大的元素进行交换
                {
                    //交换valueChange
                    double swap = Change[Row].valueChange;
                    Change[Row].valueChange = Change[i].valueChange;
                    Change[i].valueChange = swap;
                    //交换Index
                    List<int> s = new List<int>();
                    for (int t = 0; t < 2; t++)
                    {
                        s.Add(Change[Row].index[t]);
                    }
                    Change[Row].index[0] = Change[i].index[0];
                    Change[Row].index[1] = Change[i].index[1];
                    Change[i].index[0] = s[0];
                    Change[i].index[1] = s[1];
                }
            }
            for (int i = 0; i < q; i++)
            {
                int x = Change[i].index[0];
                int y = Change[i].index[1];//y不能为0
                RequestBank.Add(_solution[x].route[y]);
                if (!record.Contains(x))
                {
                    record.Add(x);
                }
            }
            for (int i = 0; i < record.Count; i++)//从_solution删除requestBank中的节点
            {
                int r = record[i];
                for (int j = 0; j < RequestBank.Count; j++)
                {
                    if (_solution[r].route.Contains(RequestBank[j]))
                    {
                        int t = _solution[r].route.IndexOf(RequestBank[j]);
                        _solution[r].route.RemoveAt(t);
                        _solution[r].visitTime.RemoveAt(t);
                        _solution[r].waitTime.RemoveAt(t);
                    }
                }
            }
            RouteRemoveChange(_solution, record);
            return RequestBank;
        }
        //效果最好
        public List<int> RelationRemove(int q, ref List<Route> _solution)//这里的时间是访问时间不是时间窗,而是实际到达时间
        {
            Route RequestBank = new Route();
            List<int> record = new List<int>();//记录变动的route，方便计算
            int a = 0;
            int b = 0;
            Random r = new Random();
            //随机选择一个节点作为最初的相似基点
            do
            {
                a = r.Next(0, _solution.Count - 1);
            } while (_solution[a].route.Count < 2);
            b = r.Next(1, _solution[a].route.Count - 1);//route第一个节点为0，故从1开始
            RequestBank.route.Add(_solution[a].route[b]);
            RequestBank.visitTime.Add(_solution[a].visitTime[b]);
            _solution[a].visitTime.RemoveAt(b);
            _solution[a].waitTime.RemoveAt(b);
            _solution[a].route.RemoveAt(b);
            record.Add(a);
            do
            {
                List<Tab> Similarity = new List<Tab>();
                a = r.Next(0, RequestBank.route.Count - 1);
                for (int i = 0; i < _solution.Count; i++)//求解其他节点与该节点的相似程度
                {
                    for (int j = 1; j < _solution[i].route.Count; j++)//route从0开始
                    {
                        //归一化操作，消除因单位不同带来的干扰
                        double X = Math.Abs(_solution[i].visitTime[j] - RequestBank.visitTime[a]) / 24;
                        double Y = Math.Abs(CustomerSet[_solution[i].route[j]].cusneed - CustomerSet[RequestBank.route[a]].cusneed) / max_minminCusneed;
                        double Z = (Cij[_solution[i].route[j]][RequestBank.route[a]] - minCusdistance) / max_minCusdistance;
                        Tab similarity = new Tab();
                        similarity.valueChange = Weights1 * X + Weights2 * Y + Weights3 * Z;
                        similarity.index.Add(i);
                        similarity.index.Add(j);
                        Similarity.Add(similarity);
                    }
                }
                int count = (int)(Math.Pow(0.9, 9) * (Similarity.Count)) + 1;
                for (int i = 0; i < count; i++)
                {
                    int Row = i;
                    double min = Similarity[i].valueChange;
                    for (int j = i + 1; j < Similarity.Count; j++)
                    {
                        if (min > Similarity[j].valueChange)
                        {
                            min = Similarity[j].valueChange;
                            Row = j;
                        }
                    }
                    if (Row != i)//此处把自身不为当前最大的元素进行交换
                    {
                        //交换valueChange
                        double swap = Similarity[Row].valueChange;
                        Similarity[Row].valueChange = Similarity[i].valueChange;
                        Similarity[i].valueChange = swap;
                        //交换Index
                        List<int> s = new List<int>();
                        for (int t = 0; t < 2; t++)
                        {
                            s.Add(Similarity[Row].index[t]);
                        }
                        Similarity[Row].index[0] = Similarity[i].index[0];
                        Similarity[Row].index[1] = Similarity[i].index[1];
                        Similarity[i].index[0] = s[0];
                        Similarity[i].index[1] = s[1];
                    }
                }
                double c = r.NextDouble();
                b = (int)(Math.Pow(c, 9/*Pr*/) * (Similarity.Count));
                int x = 0;
                int y = 0;
                x = Similarity[b].index[0];
                y = Similarity[b].index[1];//y不为0
                RequestBank.route.Add(_solution[x].route[y]);//route被删之后，similarity定位就会失败
                RequestBank.visitTime.Add(_solution[x].visitTime[y]);
                if (!record.Contains(x))
                {
                    record.Add(x);
                }
                //刷新
                _solution[x].visitTime.RemoveAt(y);
                _solution[x].waitTime.RemoveAt(y);
                _solution[x].route.RemoveAt(y);
            } while (RequestBank.route.Count < q);
            RouteRemoveChange(_solution, record);
            return RequestBank.route;
        }
        public List<int> WorstRouteRemove(int q, ref List<Route> _solution)
        {
            List<int> RequestBank = new List<int>();
            int x = 0;
            double ratio;
            int id;
            do
            {
                double max = 0;
                for (int i = 0; i < _solution.Count; i++)
                {
                    if (_solution[i].route.Count > 1)
                    {
                        ratio = _solution[i].routeCost / _solution[i].routeCapacity;
                        if (ratio > max)
                        {
                            max = ratio;
                            x = i;
                        }
                    }
                }
                //可以加入扰动
                for (int i = 1; i < _solution[x].route.Count; i++)//从1开始
                {
                    RequestBank.Add(_solution[x].route[i]);
                }
                _solution.RemoveAt(x);
            } while (RequestBank.Count < q);
            for (int i = 0; i < _solution.Count; i++)
            {
                _solution[i].ID = i;
            }
                return RequestBank;
        }
        public List<int> RouteRemove(int q, ref List<Route> _solution)
        {
            List<int> RequestBank = new List<int>();
            List<int> record = new List<int>();
            Random r = new Random();//此处可以优化
            int x;
            do
            {
                x = r.Next(0, _solution.Count - 1);
                if (_solution[x].route.Count > 1)
                {
                    for (int i = 1; i < _solution[x].route.Count; i++)//从1开始
                    {
                        RequestBank.Add(_solution[x].route[i]);
                    }
                    _solution.RemoveAt(x);
                }
            } while (RequestBank.Count < q);
            return RequestBank;
        }
        public void Repair(List<int> requestBank, ref List<Route> _solution, int b, ref bool success)//重点
        {
            List<RequesetWithBlacklist> RequestBank = new List<RequesetWithBlacklist>();
            for (int i = 0; i < requestBank.Count; i++)//初始化RequestBank
            {
                RequesetWithBlacklist Requsest = new RequesetWithBlacklist();
                Requsest.member = requestBank[i];
                RequestBank.Add(Requsest);
            }
            while (RequestBank.Count != 0)
            {
                List<Route> TwoRegret = new List<Route>();//用于储存每个待插入点的遗憾值
                //插入节点
                for (int t = 0; t < RequestBank.Count; t++)
                {
                    List<Route> BetterFeasibleRoute = new List<Route>();//用于存储每条route最好的插入形式
                    for (int i = 0; i < _solution.Count; i++)
                    {
                        if (_solution[i].routeCapacity + CustomerSet[RequestBank[t].member].cusneed <= Capacity && !RequestBank[t].blacklist.Contains(i)) //如果满足容量限制则且该route不在黑名单中，则进行插入，
                        {
                            if (_solution[i].route.Count == 1)//当route没有节点时
                            {
                                Route route = new Route();
                                route.route.Add(0);
                                route.visitTime.Add(0);
                                route.waitTime.Add(0);
                                route.route.Add(RequestBank[t].member);
                                double td = Tij[0][RequestBank[t].member];
                                if (td <= CustomerSet[RequestBank[t].member].earlyTime)
                                {
                                    route.visitTime.Add(CustomerSet[RequestBank[t].member].earlyTime);
                                    route.waitTime.Add(CustomerSet[RequestBank[t].member].earlyTime - td);
                                }
                                else
                                {
                                    route.visitTime.Add(td);
                                    route.waitTime.Add(0);
                                }
                                route.ID = _solution[i].ID;
                                route.routeCapacity = CustomerSet[RequestBank[t].member].cusneed;
                                route.routeCost = (2 * a1 + a2 * 0.001 * (2 * Car_weight + CustomerSet[RequestBank[t].member].cusneed)) * Cij[0][RequestBank[t].member] * ms;
                                route.value = route.routeCost;
                                BetterFeasibleRoute.Add(route);//此时它就是该route中最好的插入位置
                                continue;
                            }
                            else
                            {
                                List<Route> FeasibleRoute = new List<Route>();
                                bool stop = false;
                                double Time = 0;
                                double visittime = 0;
                                double waittime = 0;
                                int Case = 0;
                                int j;
                                for (j = 1; j < _solution[i].route.Count; j++)//route是从0开始的
                                {
                                    Route route = new Route();
                                    for (int i1 = 0; i1 < _solution[i].route.Count; i1++)
                                    {
                                        route.route.Add(_solution[i].route[i1]);
                                        route.visitTime.Add(_solution[i].visitTime[i1]);
                                        route.waitTime.Add(_solution[i].waitTime[i1]);
                                    }
                                    route.ID = _solution[i].ID;
                                    route.routeCapacity = _solution[i].routeCapacity;
                                    route.routeCost = _solution[i].routeCost;
                                    //为避免不必要的计算，先判断插入的合法性
                                    double d = Cij[_solution[i].route[j - 1]][RequestBank[t].member];
                                    bool f = JudgingFeasibilityOfInsertedNode(ref Time, ref visittime, ref waittime, ref stop, ref Case, RequestBank[t].member, j - 1, j, d, _solution[i]);
                                    if (!f)//此位置插入违背时间窗，
                                    {
                                        continue;
                                    }
                                    else if (stop)//之后route中不存在合法插入位置，立即停止
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        InsertionRouteChange(RequestBank[t].member, j, visittime, waittime, Time, Case, route);
                                        FeasibleRoute.Add(route);//此处有问题
                                    }
                                }
                                if (!stop)//插入到末尾试试,
                                {
                                    double Tv = _solution[i].visitTime[j - 1] + serviceTime + Tij[_solution[i].route[j - 1]][RequestBank[t].member];
                                    if (Tv <= CustomerSet[RequestBank[t].member].lastTime)//可以插入到末尾
                                    {
                                        Route route = new Route();
                                        for (int i1 = 0; i1 < _solution[i].route.Count; i1++)
                                        {
                                            route.route.Add(_solution[i].route[i1]);
                                            route.visitTime.Add(_solution[i].visitTime[i1]);
                                            route.waitTime.Add(_solution[i].waitTime[i1]);
                                        }
                                        route.ID = _solution[i].ID;
                                        route.routeCapacity = _solution[i].routeCapacity;
                                        route.routeCost = _solution[i].routeCost;
                                        if (Tv <= CustomerSet[RequestBank[t].member].earlyTime)//等待
                                        {
                                            if (CustomerSet[RequestBank[t].member].earlyTime + serviceTime + Tij[RequestBank[t].member][0] <= CustomerSet[0].lastTime)//可以回到车场
                                            {
                                                route.visitTime.Add(CustomerSet[RequestBank[t].member].earlyTime);
                                            route.waitTime.Add(CustomerSet[RequestBank[t].member].earlyTime - Tv);
                                            route.routeCapacity += CustomerSet[RequestBank[t].member].cusneed;
                                            double L, l;
                                            int k = RequestBank[t].member;
                                            L = route.routeCapacity;
                                            l = CustomerSet[k].cusneed;
                                            double cost = (a1 + a2 * 0.001 * (Car_weight + L + l)) * Cij[k][0] * ms;
                                            double increase = cost - (a1 + a2 * 0.001 * (Car_weight + L)) * (Cij[route.route.Last()][0] - Cij[route.route.Last()][k]) * ms;
                                            route.value = increase;
                                            route.routeCost += increase;
                                            route.route.Add(RequestBank[t].member);
                                            FeasibleRoute.Add(route);
                                            }
                                        }
                                        else
                                        {
                                            if (Tv + serviceTime + Tij[RequestBank[t].member][0] <= CustomerSet[0].lastTime)
                                            {
                                                route.visitTime.Add(Tv);
                                                route.waitTime.Add(0);
                                                route.routeCapacity += CustomerSet[RequestBank[t].member].cusneed;
                                                double L, l;
                                                int k = RequestBank[t].member;
                                                L = route.routeCapacity;
                                                l = CustomerSet[k].cusneed;
                                                double cost = (a1 + a2 * 0.001 * (Car_weight + L + l)) * Cij[k][0] * ms;
                                                double increase = cost - (a1 + a2 * 0.001 * (Car_weight + L)) * (Cij[route.route.Last()][0] - Cij[route.route.Last()][k]) * ms;
                                                route.value = increase;
                                                route.routeCost += increase;
                                                route.route.Add(RequestBank[t].member);
                                                FeasibleRoute.Add(route);
                                            }
                                        }
                                    }
                                }

                                if (FeasibleRoute.Count != 0)//求单个route中最佳的插入位置
                                {
                                    double min = double.MaxValue;
                                    int m = 0;
                                    for (int i1 = 0; i1 < FeasibleRoute.Count; i1++)//记录RequestBank[t]在_solution[i].route中最好的位置
                                    {
                                        if (FeasibleRoute[i1].value < min)
                                        {
                                            m = i1;
                                            min = FeasibleRoute[i1].value;
                                        }
                                    }
                                    BetterFeasibleRoute.Add(FeasibleRoute[m]);
                                }
                                else//时间窗导致的无合法插入点
                                {
                                    RequestBank[t].blacklist.Add(i);//记录下不存在合法插入点的route
                                }
                            }
                        }
                    }
                    //BetterFeasibleRoute记录RequestBank[t]在每个route里最好的插入位置
                    if (BetterFeasibleRoute.Count != 0)//说明RequestBank[t]在solution中说明里有合法插入位置
                    {
                        //一定要用冒泡法吗？对于2_regret我只想找第一与第二与第三，其他的顺序我并不关心。
                        int m1 = 0;
                        double min1 = double.MaxValue;
                        for (int i = 0; i < BetterFeasibleRoute.Count; i++)
                        {
                            if (BetterFeasibleRoute[i].value < min1)
                            {
                                m1 = i;
                                min1 = BetterFeasibleRoute[i].value;
                            }
                        }
                        TwoRegret.Add(BetterFeasibleRoute[m1]);
                        if (b == 1)
                        {
                            if (BetterFeasibleRoute.Count < 2)//此时必须优先插入
                            {
                                TwoRegret.Last().value = double.MaxValue;
                            }
                            else
                            {
                                BetterFeasibleRoute.RemoveAt(m1);
                                //找第2小的项
                                double min2 = double.MaxValue;
                                for (int i = 0; i < BetterFeasibleRoute.Count; i++)//
                                {
                                    if (BetterFeasibleRoute[i].value < min2)
                                    {
                                        min2 = BetterFeasibleRoute[i].value;
                                    }
                                }
                                if (min2 == min1)//相等时记录其值
                                {
                                    TwoRegret.Last().value = min2;  //此时value记录的是2_regret值
                                }
                                else//不等时记录差值
                                {
                                    TwoRegret.Last().value = min2 - min1;  //此时value记录的是2_regret值
                                }
                            }
                        }
                        else if (b == 2)
                        {
                            if (BetterFeasibleRoute.Count < 3)//此时必须优先插入
                            {
                                TwoRegret.Last().value = double.MaxValue;
                            }
                            else
                            {
                                BetterFeasibleRoute.RemoveAt(m1);
                                //找第2小的项
                                double min2 = double.MaxValue;
                                int m2 = 0;
                                for (int i = 0; i < BetterFeasibleRoute.Count; i++)//
                                {
                                    if (BetterFeasibleRoute[i].value < min2)
                                    {
                                        m2 = i;
                                        min2 = BetterFeasibleRoute[i].value;
                                    }
                                }
                                BetterFeasibleRoute.RemoveAt(m2);
                                //找第3小的项
                                double min3 = double.MaxValue;
                                for (int i = 0; i < BetterFeasibleRoute.Count; i++)//
                                {
                                    if (BetterFeasibleRoute[i].value < min3)
                                    {
                                        min3 = BetterFeasibleRoute[i].value;
                                    }
                                }
                                if (min3 == min1 && min2 == min1)
                                {
                                    TwoRegret.Last().value = min3;  //此时value记录的是3_regret值
                                }
                                else
                                {
                                    TwoRegret.Last().value = min3 - min1 + min2 - min1;  //此时value记录的是3_regret值
                                }
                            }
                        }
                    }
                    else//如果存在某个待插入节点在所有route中都没有合法插入位置，则无解,起到的效果较好
                    {
                        success = false;
                        break;
                    }
                }
                if (success)
                {
                    if (b == 0)//找插入成本最小的
                    {
                        double min = double.MaxValue;
                        int R1 = TwoRegret[0].ID;
                        int r1 = 0;
                        for (int i = 0; i < TwoRegret.Count; i++)
                        {
                            if (TwoRegret[i].value < min)
                            {
                                min = TwoRegret[i].value;
                                R1 = TwoRegret[i].ID;
                                r1 = i;
                            }
                        }
                        _solution.Insert(R1, TwoRegret[r1]);//
                        _solution.RemoveAt(R1 + 1);
                        //将此点从 RequestBank中移除
                        RequestBank.RemoveAt(r1);
                    }
                    else//找遗憾值最大的
                    {
                        double max = 0;
                        int R1 = 0;
                        int r1 = 0;
                        for (int i = 0; i < TwoRegret.Count; i++)
                        {
                            if (TwoRegret[i].value > max)
                            {
                                max = TwoRegret[i].value;
                                R1 = TwoRegret[i].ID;
                                r1 = i;
                            }
                        }
                        _solution.Insert(R1, TwoRegret[r1]);//
                        _solution.RemoveAt(R1 + 1);
                        //将此点从 RequestBank中移除
                        RequestBank.RemoveAt(r1);
                    }
                }
                else
                {
                    break;
                }
            }
        }
        public void Upgrade(List<Route> solution)//初始解未考虑routeCost,现在添加上
        {
            for (int i = 0; i < solution.Count; i++)
            {
                double load = 0; //接货
                int j = 0;
                for (j = 0; j < solution[i].route.Count - 1; j++)//计算新的目标值
                {
                    solution[i].routeCost += (a1 + a2 * 0.001 * (Car_weight + load)) * Cij[solution[i].route[j]][solution[i].route[j + 1]] * ms;
                    load += CustomerSet[solution[i].route[j + 1]].cusneed;
                }
                //回到车场
                solution[i].routeCost += (a1 + a2 * 0.001 * (Car_weight + load)) * Cij[solution[i].route[j]][0] * ms;
            }
        }
        public void InsertionRouteChange(int a, int c, double visitTime, double waittime, double Time, double Case, Route queue)
        {
            double oldTw;
            double Loade = CustomerSet[a].cusneed;
            if (Case == 1)//插入点后一个节点的访问时间不变，所以之后的访问时间与等待时间都不变
            {
                oldTw = queue.waitTime[c];
                queue.waitTime[c] = oldTw - Time;//等待时间缩短了
                queue.visitTime.Insert(c, visitTime);
                queue.waitTime.Insert(c, waittime);
                RouteAddCostChange(queue, c, a, Loade);
                queue.routeCapacity += CustomerSet[a].cusneed;
            }
            else
            {
                for (int i = c; i < queue.route.Count; i++)
                {
                    double tv = queue.visitTime[i] - queue.waitTime[i] + Time;
                    if (tv < CustomerSet[queue.route[i]].earlyTime)//提前到,也就是之后的节点的visitTime、waitTime不变
                    {
                        oldTw = queue.waitTime[i];
                        queue.waitTime[i] = oldTw - Time;//等待时间减少了
                        break;
                    }
                    else
                    {
                        queue.visitTime[i] = tv;
                        queue.waitTime[i] = 0;
                    }
                }
                //再插入插入节点的访问时间与等待时间
                queue.visitTime.Insert(c, visitTime);
                queue.waitTime.Insert(c, waittime);
                RouteAddCostChange(queue, c, a, Loade);
                queue.routeCapacity += CustomerSet[a].cusneed;
            }

        }
        public void RouteAddCostChange(Route _route, int a, int b, double load)//可以优化
        {
            double Load = 0;
            for (int i = 0; i < a; i++)//a-1节点时的载重
            {
                Load += CustomerSet[_route.route[i]].cusneed;
            }
            double cost = (a1 + a2 * 0.001 * (Car_weight + Load + load)) * Cij[b][_route.route[a]] * ms;
            double increase = cost - (a1 + a2 * 0.001 * (Car_weight + Load)) * (Cij[_route.route[a - 1]][_route.route[a]] - Cij[_route.route[a - 1]][b]) * ms;
            for (int i = a; i < _route.route.Count - 1; i++)//计算新的目标值
            {
                increase += (a2 * 0.001 * load) * Cij[_route.route[i]][_route.route[i + 1]] * ms;
            }
            increase += (a2 * 0.001 * load) * Cij[_route.route.Last()][0] * ms;
            _route.routeCost += increase;
            _route.value = increase;
            _route.route.Insert(a, b);
        }
        public void RouteRemoveChange(List<Route> solution, List<int> a)
        {
            for (int i = 0; i < solution.Count; i++)
            {
                double td;//行驶时间
                if (a.Contains(i))
                {
                    if (solution[i].route.Count > 1)
                    {
                        int j;
                        solution[i].routeCapacity = 0;
                        solution[i].routeCost = 0;
                        //从车场出发到达第一个节点的状态
                        solution[i].routeCost += (a1 + a2 * 0.001 * Car_weight) * Cij[0][solution[i].route[1]] * ms;//碳排放
                        double Tmin = CustomerSet[solution[i].route[1]].earlyTime;
                        if (Tij[0][solution[i].route[1]] <= Tmin)
                        {
                            solution[i].visitTime[1] = Tmin;
                            solution[i].waitTime[1] = Tmin - Tij[0][solution[i].route[1]];
                        }
                        else
                        {
                            solution[i].visitTime[1] = Tij[0][solution[i].route[1]];
                            solution[i].waitTime[1] = 0;
                        }
                        solution[i].routeCapacity += CustomerSet[solution[i].route[1]].cusneed;
                        for (j = 1; j < solution[i].route.Count - 1; j++)
                        {
                            solution[i].routeCost += (a1 + a2 * 0.001 * (Car_weight + solution[i].routeCapacity)) * Cij[solution[i].route[j]][solution[i].route[j + 1]] * ms;//碳排放
                            double Ta = solution[i].visitTime[j] + serviceTime + Tij[solution[i].route[j]][solution[i].route[j + 1]];//此处有问题
                            Tmin = CustomerSet[solution[i].route[j + 1]].earlyTime;
                            if (Ta <= Tmin)
                            {
                                solution[i].visitTime[j + 1] = Tmin;
                                solution[i].waitTime[j + 1] = Tmin - Ta;
                            }
                            else
                            {
                                solution[i].visitTime[j + 1] = Ta;
                                solution[i].waitTime[j + 1] = 0;
                            }
                            solution[i].routeCapacity += CustomerSet[solution[i].route[j + 1]].cusneed;
                        }
                        solution[i].routeCost += (a1 + a2 * 0.001 * (Car_weight + solution[i].routeCapacity)) * Cij[solution[i].route[j]][0] * ms;//碳排放
                    }
                }
            }
            for (int i = 0; i < solution.Count; i++)//刷新ID值
            {
                if (solution[i].route.Count < 1)
                {
                    solution.RemoveAt(i);
                    i--;
                }
                else
                {
                    solution[i].ID = i;
                }
            }
        }
        public double WorstRemoveChange(Route _route, int a)//移除一个节点的cost变动
        {
            double Load = 0;
            double load = CustomerSet[_route.route[a]].cusneed;
            for (int i = 0; i < a; i++)//a-1节点时的载重
            {
                Load += CustomerSet[_route.route[i]].cusneed;
            }
            double costChange;
            double cost;
            if (a == _route.route.Count - 1) //删除最后一位
            {
                cost = (a1 + a2 * 0.001 * (Car_weight + Load)) * (Cij[_route.route[a - 1]][_route.route[0]] - Cij[_route.route[a - 1]][_route.route[a]]) * ms;
                costChange = (a1 + a2 * 0.001 * (Car_weight + Load + load)) * Cij[_route.route[a]][_route.route[0]] * ms - cost;
            }
            else
            {
                cost = (a1 + a2 * 0.001 * (Car_weight + Load)) * (Cij[_route.route[a - 1]][_route.route[a + 1]] - Cij[_route.route[a - 1]][_route.route[a]]) * ms;
                costChange = (a1 + a2 * 0.001 * (Car_weight + Load + load)) * Cij[_route.route[a]][_route.route[a + 1]] * ms - cost;
                int j;
                for (j = a + 1; j < _route.route.Count - 1; j++)//计算新的目标值
                {
                    costChange += (a2 * 0.001 * load) * Cij[_route.route[j]][_route.route[j + 1]] * ms;
                }
                costChange += (a2 * 0.001 * load) * Cij[j][0] * ms;
            }
            return costChange;
        }
        public double SolutionCost(List<Route> queue)
        {
            double cost = 0;
            int i;
            for (i = 0; i < queue.Count; i++)
            {
                cost += queue[i].routeCost;
            }
            return cost;
        }
        public List<int> KruskalClassification(Route route)
        {
            List<int> cluster = new List<int>();
            int[] record = new int[2];
            double[] max = new double[] { 0, 0, 0 };
            for (int i = 0; i < 2; i++)
            {
                int j;
                for (j = 0; j < route.route.Count - 1; j++)
                {
                    if (max[i] != Cij[route.route[j]][route.route[j + 1]] && max[i + 1] < Cij[route.route[j]][route.route[j + 1]])
                    {
                        max[i + 1] = Cij[route.route[j]][route.route[j + 1]];
                        record[i] = j;
                    }
                }
                if (max[i] != Cij[route.route[j]][0] && max[i + 1] < Cij[route.route[j]][0])
                {
                    max[i + 1] = Cij[route.route[j]][0];
                    record[i] = j;
                }
            }
            if (record[0] > record[1])
            {
                int temp = record[0];
                record[0] = record[1];
                record[1] = temp;
            }
            Random r = new Random();
            int t = r.Next(0, 1);
            if (false)
            {
                for (int i = 1; i < record[0] + 1; i++)
                {
                    cluster.Add(route.route[i]);
                }
                for (int i = record[1] + 1; i < route.route.Count; i++)
                {
                    cluster.Add(route.route[i]);
                }
                for (int i = 0; i < cluster.Count; i++)
                {
                    route.route.Remove(cluster[i]);
                }
            }
            else
            {
                for (int i = record[0] + 1; i < record[1] + 1; i++)
                {
                    cluster.Add(route.route[i]);
                }
                route.route.RemoveRange(record[0] + 1, record[1] - record[0]);
            }
            return cluster;
        }
    }
}

