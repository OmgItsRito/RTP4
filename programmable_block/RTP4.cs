public static class RTP4{static IMyGridProgramRuntimeInfo a;static List<X>b;static IMyRadioAntenna[]c;static MyTransmitTarget
d;static int e;static int f;static Func<string,byte,bool>g;static Action<IConnection>h;static string i;static string j;static
long k;public static bool IsInitialized{get{return b!=null;}}public static string LocalName{get{return i;}}public static void
Initialize(IMyGridProgramRuntimeInfo m,IMyRadioAntenna[]n,MyTransmitTarget o,string p,int q=5,int r=400,Func<string,byte,bool
>s=null,Action<IConnection>t=null){if(b==null){if(m==null){throw new Exception("runtime can not be null");}if(n==null||n.
Length==0){throw new Exception("antennas can not be null or have length of 0");}if(p==null||p.Length<=0||p.Length>99){throw
new Exception("localName length must be between 1 and 9 inclusive");}a=m;c=(IMyRadioAntenna[])n.Clone();i=p;e=q<=0?1:q;f=r<0?
0:r;g=s;h=t;b=new List<X>();b.Add(new X());d=o;j=i.Length.ToString("00");k=r*3;for(int i=0,l=c.Length;i<l;i++){
IMyRadioAntenna a=c[i];if(o==MyTransmitTarget.Owned){a.IgnoreAlliedBroadcast=true;a.IgnoreOtherBroadcast=true;}else if(o==
MyTransmitTarget.Ally){a.IgnoreAlliedBroadcast=false;a.IgnoreOtherBroadcast=true;}else if(o==MyTransmitTarget.Enemy||o==
MyTransmitTarget.Everyone){a.IgnoreAlliedBroadcast=false;a.IgnoreOtherBroadcast=false;}}}}public static void StaticUpdate(){k
+=a.TimeSinceLastRun.Milliseconds;List<Z>e=null;int f=0;b[0].A(ref e,ref f);for(int i=b.Count-1;i>0;i--){b[i].A(ref e,ref f);
}if(e!=null){StringBuilder pb=new StringBuilder(6+f);pb.Append("[RTP4]");for(int i=0,l=e.Count;i<l;i++){pb.Append(e[i].a);}
string g=pb.ToString();for(int j=0,m=c.Length;j<m;j++){if(c[j].TransmitMessage(g,d)){for(int i=0,l=e.Count;i<l;i++){e[i].F();
}return;}}}}public static void OnAntennaMessage(string a){if(a!=null){int b=a.Length;if(b>6&&a.StartsWith("[RTP4]")){int c=6,
d;while(c<b){if(c+3<b&&int.TryParse(a.Substring(c,3),out d)){if(d<=b-c-3){if(Z.D(a,c+3,d+c+3)){c+=3+d;continue;}}}break;}}}}
public static IConnection OpenConnection(string a,byte d){if(a==null||a.Length<=0||a.Length>99){return null;}if(
GetExistingConnection(a,d)==null){Y c=new Y();c.d=a;c.e=d;b.Add(c);c.a.AddLast(Z.A(a,d,'d',1,c.E));return c;}return null;}
public static IConnection GetExistingConnection(string a,byte d){for(int i=1,l=b.Count;i<l;i++){Y c=b[i]as Y;if(c.e==d&&c.d==
a){return c;}}return null;}public static void Shutdown(){if(b!=null){for(int i=0,l=b.Count;i<l;i++){b[i].Close();}k=long.
MaxValue;StaticUpdate();a=null;b.Clear();b=null;c=null;g=null;h=null;}}public interface IConnection{string Target{get;}byte
Channel{get;}Action OnOpen{get;set;}Action<string>OnData{get;set;}Action OnClose{get;set;}bool IsOpen{get;}int
DataQueueLength{get;}void SendData(string data,bool reliable=true);void Close();[Obsolete("Untested Code")]void GetQueuedData
(List<string>dataList);}class X{public LinkedList<Z>a=new LinkedList<Z>();public virtual void A(ref List<Z>b,ref int c){if(a.
Count>0){LinkedListNode<Z>d=a.First;do{Z p=d.Value;if(d==d.Next){d=null;}else{d=d.Next;}if(p.E()&&c+p.a.Length<90000){if(b==
null){b=new List<Z>();}b.Add(p);c+=p.a.Length;}else if(d==null){d=a.First;}}while(d!=null&&d!=a.First);}}public virtual void
Close(){a.Clear();}public virtual void B(Z p){a.Remove(p);}}class Y:X,IConnection{public List<Z>c=new List<Z>(4);public
string d;public byte e;public string Target{get{return d;}}public byte Channel{get{return e;}}public Action OnOpen{get;set;}
public Action<string>OnData{get;set;}public Action OnClose{get;set;}public byte f=23;public bool IsOpen{get{return f==25;}}
public int DataQueueLength{get{LinkedListNode<Z>b=a.First;int l=0,c;while(b!=null&&b!=a.First){c=b.Value.c;if(c>=100||c==50){
l++;}b=b.Next;}return l;}}public int C{get{return++g>=1000?(g=100):g;}}int g=99;public int h=100;public override void Close()
{if(f!=27){D();Z p=Z.A(d,e,'c',C,b[0].B);p.l=1;b[0].a.AddLast(p);}}public void D(){b.Remove(this);c.Clear();c=null;f=27;if(
OnClose!=null){OnClose.Invoke();OnClose=null;}OnOpen=null;OnData=null;}public void SendData(string b,bool c){if(f==25){if(b==
null){b="";}else if(b.Length>850){return;}if(c){a.AddLast(Z.B(this,b,C,E));}else{a.AddLast(Z.B(this,b,50,B));}}}public void
GetQueuedData(List<string>b){b.EnsureCapacity(b.Count+a.Count);LinkedListNode<Z>c=a.First;do{string d=c.Value.a;int a=int.
Parse(d.Substring(3,2));a=3+2+a+2+i.Length+3+3+1;b.Add(d.Substring(a,d.Length-a));c=c.Next;}while(c!=a.First);}public
override void A(ref List<Z>b,ref int d){if(a.Count>0){Z p=a.First.Value;if(p.E()){if(d+p.a.Length<90000){if(b==null){b=new
List<Z>();}b.Add(p);d+=p.a.Length;}}else if(f==27){return;}}if(c.Count>0){if(b==null){b=new List<Z>();}int i=0,l=c.Count,p;
for(;i<l;i++){p=c[i].a.Length;if(d+p<90000){d+=p;}else{break;}}if(i==l){b.AddList(c);}else{l=0;b.EnsureCapacity(b.Count+i);
for(;l<i;l++){b.Add(c[l]);}}}}public void E(Z p){Close();}public void F(Z p){c.Remove(p);}}class Z{public static Z A(string a
,byte b,char c,int d,Action<Z>f){Z p=new Z();p.c=d;p.l=e;string g=a.Length.ToString("00")+a+j+i+b.ToString("000")+d.ToString(
"000")+c;p.a=string.Concat(g.Length.ToString("000"),g);p.d=f;return p;}public static Z B(Y a,string b,int c,Action<Z>d){Z p=
new Z();p.c=c;p.l=e;string f=a.d.Length.ToString("00")+a.d+j+i+a.e.ToString("000")+c.ToString("000")+'b'+b;p.a=string.Concat(
f.Length.ToString("000"),f);p.d=d;return p;}public static Z C(Y a,int b){Z p=new Z();p.c=0;string c=a.d.Length.ToString("00")
+a.d+j+i+a.e.ToString("000")+"000"+b.ToString("000");p.a=string.Concat(c.Length.ToString("000"),c);p.d=a.F;return p;}public
static bool D(string a,int d,int e){int f;string j;if(d+2<e&&int.TryParse(a.Substring(d,2).ToString(),out f)){d+=2;if(d+f<e){
j=a.Substring(d,f);if(j==i){d+=f;if(d+2<e&&int.TryParse(a.Substring(d,2).ToString(),out f)){d+=2;if(d+f<e){j=a.Substring(d,f)
;d+=f;byte k;if(d+3<e&&byte.TryParse(a.Substring(d,3),out k)){d+=3;if(d+3<e&&int.TryParse(a.Substring(d,3),out f)){d+=3;if(f
==0){Y c=GetExistingConnection(j,k)as Y;if(c!=null){if(d+3<=e&&int.TryParse(a.Substring(d,3),out f)){LinkedListNode<Z>node=c.
a.First;if(node.Value.c==f){c.a.RemoveFirst();}else{while((node=node.Next)!=c.a.First){if(node.Value.c==f){c.a.Remove(node);
break;}}}}else{return false;}}}else if(d<e){char m=a[d++];Y c=GetExistingConnection(j,k)as Y;if(c==null){if(m=='d'){if(g!=
null&&g.Invoke(j,k)){c=new Y();c.d=j;c.e=k;c.a.AddLast(A(j,k,'e',1,c.E));b.Add(c);if(h!=null){h.Invoke(c);}}else{Z p=A(j,k,
'c',1,b[0].B);p.l=1;b[0].a.AddLast(p);}}}else{if(m=='b'){if(c.f==25){if(f==50||c.h==f){if(f!=50){if(++c.h>999){c.h=100;}}int
l=e-d;if(l<0){return false;}if(l==0){if(c.OnData!=null){c.OnData.Invoke("");}}else if(d+l<=e){if(c.OnData!=null){c.OnData.
Invoke(a.Substring(d,l));}}else{return false;}}if(f>=100){c.c.Add(C(c,f));}}}else if(m=='d'){if(c.f==23){c.a.Clear();c.a.
AddLast(A(j,k,'e',1,c.E));}}else if(m=='e'){Z p=A(j,k,'f',1,c.B);p.l=1;if(c.f==23){c.a.Clear();c.a.AddLast(p);c.f=25;if(c.
OnOpen!=null){c.OnOpen.Invoke();}}else{c.a.AddFirst(p);}}else if(m=='f'){if(c.f==23){c.a.Clear();c.f=25;if(c.OnOpen!=null){c.
OnOpen.Invoke();}}}else if(m=='c'&&f>=100){if((c.f==23||c.h==f)&&c.f!=27){c.D();}}}}else{return false;}return true;}}}}}}}
return false;}public string a;public int c;public Action<Z>d;public int l;public long m;public bool E(){if(c==50){return true
;}if(l<=0){d.Invoke(this);return false;}return k-m>=f;}public void F(){if(c==0||c==50){d.Invoke(this);}else{m=k;l--;}}}}
