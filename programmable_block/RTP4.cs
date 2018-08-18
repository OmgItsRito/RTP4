public static class RTP4{static IMyGridProgramRuntimeInfo a;static List<A>b;static IMyRadioAntenna[]c;static MyTransmitTarget
d;static int e;static int f;static Func<string,byte,bool>g;static Action<IConnection>h;static string i;static int j;static
long k;public static bool IsInitialized{get{return b!=null;}}public static string LocalName{get{return i;}}public static void
Initialize(IMyGridProgramRuntimeInfo m,IMyRadioAntenna[]n,MyTransmitTarget o,string p,int r,int s,Func<string,byte,bool>t,
Action<IConnection>u){if(b==null){if(m==null){throw new Exception("runtime can not be null");}if(n==null||n.Length==0){throw
new Exception("antennas can not be null or have length of 0");}if(p==null||p.Length<=0||p.Length>9){throw new Exception(
"localName length must be between 1 and 9 inclusive");}if(t==null){throw new Exception(
"connectionAcceptor delegate can not be null");}a=m;c=(IMyRadioAntenna[])n.Clone();i=p;e=r<=0?1:r;f=s<0?0:s;g=t;h=u;b=new
List<A>();b.Add(new A());d=o;j=i.Length;k=s*3;for(int i=0,l=c.Length;i<l;i++){IMyRadioAntenna a=c[i];if(o==MyTransmitTarget.
Owned){a.IgnoreAlliedBroadcast=true;a.IgnoreOtherBroadcast=true;}else if(o==MyTransmitTarget.Ally){a.IgnoreAlliedBroadcast=
false;a.IgnoreOtherBroadcast=true;}else if(o==MyTransmitTarget.Enemy||o==MyTransmitTarget.Everyone){a.IgnoreAlliedBroadcast=
false;a.IgnoreOtherBroadcast=false;}}}}public static void StaticUpdate(){k+=a.TimeSinceLastRun.Milliseconds;List<C>e=null;int
f=0;b[0].c(ref e,ref f);for(int i=b.Count-1;i>0;i--){b[i].c(ref e,ref f);}if(e!=null){StringBuilder g=new StringBuilder(6+f);
g.Append("[RTP4]");for(int i=0,l=e.Count;i<l;i++){g.Append(e[i].m);}string msg=g.ToString();for(int j=0,m=c.Length;j<m;j++){
if(c[j].TransmitMessage(msg,d)){for(int i=0,l=e.Count;i<l;i++){e[i].c();}return;}}}}public static void OnAntennaMessage(
string a){if(a!=null){int b=a.Length;if(b>6&&a.StartsWith("[RTP4]")){int c=6,d;while(c<b){if(c+3<b&&int.TryParse(a.Substring(
c,3),out d)){if(d<=b-c-3){if(C.l(a,c+3,d+c+3)){c+=3+d;continue;}}}break;}}}}public static IConnection OpenConnection(string a
,byte d){if(a==null||a.Length<=0||a.Length>9){return null;}if(GetExistingConnection(a,d)==null){B c=new B();c.e=a;c.f=d;b.Add
(c);c.a.AddLast(C.a(a,d,'d',1,c.l));return c;}return null;}public static IConnection GetExistingConnection(string a,byte d){
for(int i=1,l=b.Count;i<l;i++){B c=b[i]as B;if(c.f==d&&c.e==a){return c;}}return null;}public static void Shutdown(){if(b!=
null){for(int i=0,l=b.Count;i<l;i++){b[i].Close();}k=long.MaxValue;StaticUpdate();a=null;b.Clear();b=null;c=null;g=null;h=
null;}}public interface IConnection{string Target{get;}byte Channel{get;}Action OnOpen{get;set;}Action<string>OnData{get;set;
}Action OnClose{get;set;}bool IsOpen{get;}void SendData(string data);void Close();}class A{public LinkedList<C>a=new
LinkedList<C>();public virtual void c(ref List<C>b,ref int c){if(a.Count>0){LinkedListNode<C>d=a.First;do{C p=d.Value;if(d==d
.Next){d=null;}else{d=d.Next;}if(p.a()&&c+p.m.Length<90000){if(b==null){b=new List<C>();}b.Add(p);c+=p.m.Length;}else if(d==
null){d=a.First;}}while(d!=null&&d!=a.First);}}public virtual void Close(){a.Clear();}public virtual void d(C p){a.Remove(p);
}}class B:A,IConnection{public List<C>g=new List<C>(4);public string e;public byte f;public string Target{get{return e;}}
public byte Channel{get{return f;}}public Action OnOpen{get;set;}public Action<string>OnData{get;set;}public Action OnClose{
get;set;}public byte h=23;public bool IsOpen{get{return h==25;}}public int i{get{return++j>=1000?(j=100):j;}}int j=99;public
int k=100;public override void Close(){if(h!=27){c();C p=C.a(e,f,'c',i,b[0].d);p.p=1;b[0].a.AddLast(p);}}public void c(){b.
Remove(this);a.Clear();g.Clear();a=null;g=null;h=27;if(OnClose!=null){OnClose.Invoke();OnClose=null;}OnOpen=null;OnData=null;
}public void SendData(string b){if(h==25){if(b==null){b="";}else if(b.Length>900){return;}a.AddLast(C.c(this,b,i));}}public
override void c(ref List<C>b,ref int c){if(a.Count>0){C p=a.First.Value;if(p.a()){if(c+p.m.Length<90000){if(b==null){b=new
List<C>();}b.Add(p);c+=p.m.Length;}}else if(h==27){return;}}if(g.Count>0){if(b==null){b=new List<C>();}int i=0,l=g.Count,p;
for(;i<l;i++){p=g[i].m.Length;if(c+p<90000){c+=p;}else{break;}}if(i==l){b.AddList(g);}else{l=0;b.EnsureCapacity(b.Count+i);
for(;l<i;l++){b.Add(g[l]);}}}}public void l(C p){Close();}public void m(C p){g.Remove(p);}}class C{public static C a(string a
,byte b,char c,int d,Action<C>f){C p=new C();p.n=d;p.p=e;string g=a.Length+a+j+i+b.ToString("000")+d.ToString("000")+c;p.m=
string.Concat(g.Length.ToString("000"),g);p.o=f;return p;}public static C c(B a,string b,int c){C p=new C();p.n=c;p.p=e;
string d=a.e.Length+a.e+j+i+a.f.ToString("000")+c.ToString("000")+'b'+b;p.m=string.Concat(d.Length.ToString("000"),d);p.o=a.l
;return p;}public static C d(B a,int b){C p=new C();p.n=0;string c=a.e.Length+a.e+j+i+a.f.ToString("000")+"000"+b.ToString(
"000");p.m=string.Concat(c.Length.ToString("000"),c);p.o=a.m;return p;}public static bool l(string e,int f,int j){int k,m=f;
string n;if(m+1<j&&int.TryParse(e[m++].ToString(),out k)){if(m+k<j){n=e.Substring(m,k);if(n==i){m+=k;if(m+1<j&&int.TryParse(e
[m++].ToString(),out k)){if(m+k<j){n=e.Substring(m,k);m+=k;byte channel;if(m+3<j&&byte.TryParse(e.Substring(m,3),out channel)
){m+=3;if(m+3<j&&int.TryParse(e.Substring(m,3),out k)){m+=3;B c=GetExistingConnection(n,channel)as B;if(k==0){if(c!=null){if(
m+3<=j&&int.TryParse(e.Substring(m,3),out k)){LinkedListNode<C>node=c.a.First;if(node.Value.n==k){c.a.RemoveFirst();}else{
while((node=node.Next)!=c.a.First){if(node.Value.n==k){c.a.Remove(node);break;}}}}else{return false;}}}else if(m<j){char o=e[
m++];if(c==null){if(o=='d'){if(g.Invoke(n,channel)){c=new B();c.e=n;c.f=channel;c.a.AddLast(a(n,channel,'e',1,c.l));b.Add(c);
if(h!=null){h.Invoke(c);}}else{C p=a(n,channel,'c',1,b[0].d);p.p=1;b[0].a.AddLast(p);}}}else{if(o=='b'){if(k>=100&&c.h==25){
if(c.k==k){if(++c.k>999){c.k=100;}int l=j-m;if(l<0){return false;}if(l==0){if(c.OnData!=null){c.OnData.Invoke("");}}else if(m
+l<=j){if(c.OnData!=null){c.OnData.Invoke(e.Substring(m,l));}}else{return false;}}c.g.Add(d(c,k));}}else if(o=='d'){if(c.h==
23){c.a.Clear();c.a.AddLast(a(n,channel,'e',1,c.l));}}else if(o=='e'){C p=a(n,channel,'f',1,c.d);p.p=1;c.a.AddLast(p);if(c.h
==23){c.a.Clear();c.h=25;if(c.OnOpen!=null){c.OnOpen.Invoke();}}}else if(o=='f'){if(c.h==23){c.a.Clear();c.h=25;if(c.OnOpen!=
null){c.OnOpen.Invoke();}}}else if(o=='c'&&k>=100){if((c.h==23||c.k==k)&&c.h!=27){c.c();}}}}else{return false;}return true;}}
}}}}}return false;}public string m;public int n;public Action<C>o;public int p;public long q;public bool a(){if(p<=0){o.
Invoke(this);return false;}return k-q>=f;}public void c(){if(n==0){o.Invoke(this);}else{q=k;p--;}}}}
