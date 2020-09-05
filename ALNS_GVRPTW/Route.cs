using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/* 
  @Author：Created by 张平 from 东北大学
  @Email:1257524054@qq.com
  @Date：Created in 2020.0504
 */
namespace ALNS_GVRPTW
{
    public class Route/*:System.Object, ICloneable*///可以用get、set
    {
        public int ID;
        public int idDepot;//车场号
        public  List<int> route = new List<int>();//route的第一位为0
        public  List<double > visitTime = new List<double>();//记录访问每个节点的时刻
        public  List<double> waitTime = new List<double>();//每个节点的等待时间
        public double routeCost ;//碳排放
        public double routeCapacity;//容量
        public double value;//用于记录插入后的routeCost的变动，同时也被用于储存regret值
    }
}
