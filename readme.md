## 版本更新

##### 2022-1-17 可以抢大麦和保利的票，只提供抢一张票的功能。大麦只能抢需要选座的演出

## 配置

![0805a7b78b5f5fa3f1c4a87002ac8c98.png](en-resource://database/1213:0)
抢大麦的票需要nodejs环境,因为要通过js代码加密sign

### 待更新

##### 完善获取代理IP方法

* * *

### 保利

#### 抢票界面演出信息接口

https://platformpcgateway.polyt.cn/api/1.0/show/getShowInfoDetail

#### 返回所有位置信息

https://cdn.polyt.cn/seat/h5/57810_57606.json?callback=jsonpCallback

#### 返回所有可购买作为Id

https://platformpcgateway.polyt.cn/api/1.0/seat/getSellSeatList

无论是移动端还是pc端的接口都有限流,每个线程抢票间隔为1.5s,单个IP请求太频繁会被禁IP



* * *

### 大麦



抢大麦的票需要nodejs环境,因为要通过js代码加密sign



#### 获取界面演出信息接口

https://detail.damai.cn/subpage?itemId=664009496699&apiVersion=2.0&dmChannel=pc@damai_pc&bizCode=ali.china.damai&scenario=itemsku&dataType=&dataId=&privilegeActId=&callback=__jp0



#### 获取区域分组信息接口

mtop.damai.wireless.project.getb2b2careainfo

#### 查询座位信息

##### 当区域只有一个时：

mtop.damai.wireless.seat.queryseatstatus或

##### 当区域有多个时：

①mtop.damai.wireless.seat.queryperformseatstatus,返回值中2代表有座，8代表无座(X, 
8)代表连续X个为无座

②https://sseat.damai.cn/xuanzuo/io/110100/1936836267/10000072/6028214.json,/城市id/xxx/xxx/groupid(区域分组id),/xxx/xxx/在mtop.damai.wireless.seat.queryseatstatus接口返回值中找



pfId的计算在g.alcdn.com/damai/pc-seat/0.0.5/pc.js 的getAreaInfo方法中

sign的加密算法在g.alcdn.com/damai/pc-seat/0.0.5/vendor.js的26813行左右, 其中参数的token值使用正则从cookie提取,代码在vendor.js的26500行左右，目前已将加密js方法复制到本地执行



目前来看，以下两值不同演出下均可通用,至少可以维持很长一段时间有效

umidToken的加密算法在g.alcdn.com/AWSC/WebUMID/1.88.4/um.js里,也可在本地Local 
Storage内查找_um_cn_umsvtn值

ua的加密算法在g.alcdn.com/AWSC/uab/1.140.0/1.88.4/collina.js,或者调试中断点获取



大麦接口貌似没有访问限制, Cookie的有效期不长,如果调用接口失败很大可能是Cookie过期了



* * *
我会时不时更新，有疑问的地方+v:**liaohan918**
