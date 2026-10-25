import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const artifactToolModule = process.env.LABGUARD_ARTIFACT_TOOL_MODULE || 'file:///C:/Users/lyle3/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const { FileBlob, PresentationFile } = await import(artifactToolModule);

const out = path.dirname(fileURLToPath(import.meta.url)) + path.sep;
const source = process.env.LABGUARD_SOURCE_PPTX || path.join(out, 'LabGuard_Nhom5_GK_Keynote_SOURCE.pptx');
await fs.mkdir(out,{recursive:true});
const C={bg:'#F5F4F0',ink:'#152334',navy:'#101C2B',navy2:'#1A2B3B',teal:'#168F88',teal2:'#DCEFEB',amber:'#C88227',amber2:'#F6E9D5',gray:'#647487',gray2:'#E2E6E7',gray3:'#AAB4BA',white:'#FFFFFF',red:'#A94E43',green:'#337B63'};
const FONT='Arial';
function rect(slide,name,x,y,w,h,fill,stroke='none',sw=0,r=0){
 const sh=slide.shapes.add({geometry:r?'roundRect':'rect',name,position:{left:x,top:y,width:w,height:h},fill,line:{style:'solid',fill:stroke,width:sw},...(r?{borderRadius:r}:{})}); return sh;
}
function txt(slide,name,x,y,w,h,value,size,color=C.ink,opt={}){
 const sh=slide.shapes.add({geometry:'textbox',name,position:{left:x,top:y,width:w,height:h},fill:'none',line:{style:'solid',fill:'none',width:0}});
 sh.text=value;
 sh.text.style={fontSize:size,typeface:FONT,color,bold:!!opt.bold,italic:!!opt.italic,alignment:opt.align||'left',verticalAlignment:opt.valign||'middle',autoFit:opt.autoFit||'shrinkText',wrap:'square',insets:opt.insets??0};
 return sh;
}
function line(slide,name,x1,y1,x2,y2,color,width=2,style='solid'){
 const left=Math.min(x1,x2), top=Math.min(y1,y2), w=Math.max(1,Math.abs(x2-x1)), h=Math.max(1,Math.abs(y2-y1));
 const negativeSlope=(x2-x1)*(y2-y1)<0;
 return slide.shapes.add({geometry:'line',name,position:{left,top,width:w,height:h,horizontalFlip:negativeSlope},fill:'none',line:{style,fill:color,width}});
}
function arrow(slide,name,x,y,w=36,h=24,color=C.teal){
 return slide.shapes.add({geometry:'rightArrow',name,position:{left:x,top:y,width:w,height:h},fill:color,line:{style:'solid',fill:'none',width:0}});
}
function header(slide,n,title,subtitle='',dark=false){
 const primary=dark?C.white:C.ink, secondary=dark?'#AFC0C9':C.gray;
 txt(slide,`eyebrow-${n}`,64,35,500,25,`LABGUARD  ·  NT106  ·  ${String(n).padStart(2,'0')} / 09`,17,dark?'#75C8BE':C.teal,{bold:true});
 txt(slide,`title-${n}`,64,70,1150,60,title,40,primary,{bold:true});
 if(subtitle) txt(slide,`subtitle-${n}`,66,132,1140,34,subtitle,22,secondary);
 if(n!==2) txt(slide,`footer-${n}`,930,680,285,20,'UIT  ·  NHÓM 5',15,secondary,{align:'right'});
}
function pill(slide,name,x,y,w,label,fill,color,stroke='none'){
 rect(slide,name,x,y,w,36,fill,stroke,1,18); txt(slide,name+'-text',x+9,y+1,w-18,34,label,17,color,{bold:true,align:'center'});
}
function addNotes(slide,content){slide.speakerNotes.text=content;}

const p=await PresentationFile.importPptx(await FileBlob.load(source));
const original=[...p.slides.items];
const labScene=original[3];
for(const s of original) if(s!==labScene) s.delete();
const s1=p.slides.add(); s1.moveTo(0);
const s2=labScene;
// Scene 2 reuses the original native isometric illustration and its staged MAY-03 reveal.
const roomTitle=s2.shapes.items.find(sh=>sh.name==='heading');
if(!roomTitle) throw new Error('Original classroom scene heading was not found.');
roomTitle.text.set('40 máy tính —\nmột máy cần chú ý');
roomTitle.text.fontSize=28;
roomTitle.position={...roomTitle.position,height:80};
txt(s2,'rework-room-scale-label',70,575,600,32,'10 máy trong hình đại diện quy mô đề xuất khoảng 40 máy',18,'#8BCFC5',{bold:true});
addNotes(s2,`TIME: 30 giây\nSAY: Giả sử phòng thực hành có khoảng 40 máy; các máy trong hình chỉ là đại diện. Trong tình huống minh họa, MAY-03 có ba mẫu CPU liên tiếp vượt ngưỡng giả định. Người trực chưa thể kết luận máy hỏng; trước hết cần kiểm tra ảnh hưởng đến buổi học và xem thêm bằng chứng.\nPOINT: Cảnh báo giúp ưu tiên nơi cần kiểm tra, nhưng không tự kết luận nguyên nhân.\nTRANSITION: Để theo dõi MAY-03 từ xa, dữ liệu của các máy cần có một đường đi tập trung.\nCLICK CUE: Dùng các nhịp bấm có sẵn để làm nổi bật MAY-03 và hạ độ chú ý của các máy còn lại.\nTECHNICAL CAUTION: Quy mô 40 máy và các mẫu CPU đều là tình huống thiết kế giả định, không phải triển khai hay đo đạc thực tế.`);

// Slide 1 — problem and identity.
s1.background.fill=C.bg;
txt(s1,'s1-kicker',70,44,700,30,'UIT  ·  NT106 — LẬP TRÌNH MẠNG CĂN BẢN',20,C.gray,{bold:true});
txt(s1,'s1-wordmark',66,155,650,112,'LABGUARD',84,C.ink,{bold:true});
txt(s1,'s1-subtitle',72,268,650,72,'Giám sát và quản trị\nphòng máy tập trung',37,C.ink,{bold:true});
line(s1,'s1-rule',72,376,132,376,C.teal,4);
txt(s1,'s1-question',72,402,640,98,'Trong phòng thực hành ~40 máy,\nmáy nào cần kiểm tra trước?',31,C.ink,{bold:true});
txt(s1,'s1-status',72,520,600,30,'ĐỀ XUẤT THIẾT KẾ  ·  BÁO CÁO GIỮA KỲ',18,C.teal,{bold:true});
// Agent-to-Central motif: three labeled inputs converge to one named authority.
const ax=770, yrows=[223,313,403];
for(let i=0;i<3;i++){
 rect(s1,`s1-agent-mark-${i}`,ax,yrows[i],150,52,'#E7ECEC',C.gray3,1,3);
 rect(s1,`s1-agent-dot-${i}`,ax+14,yrows[i]+17,16,16,i===1?C.teal:C.gray, 'none',0,8);
 txt(s1,`s1-agent-label-${i}`,ax+42,yrows[i]+4,100,42,['Agent 01','Agent 02','Agent …'][i],18,C.ink,{bold:i===1});
 line(s1,`s1-agent-link-${i}`,ax+150,yrows[i]+26,1012,330,i===1?C.teal:C.gray3,i===1?3:2,'solid');
}
rect(s1,'s1-central-body',1012,274,174,126,C.navy,'none',0,8);
for(let i=0;i<3;i++){rect(s1,`s1-central-slot-${i}`,1037,296+i*22,88,4,'#DCE4E8','none',0,0);rect(s1,`s1-central-light-${i}`,1150,291+i*22,10,10,C.teal,'none',0,5);}
txt(s1,'s1-central-label',1000,410,200,34,'CENTRAL',24,C.ink,{bold:true,align:'center'});
txt(s1,'s1-motif-caption',760,470,430,36,'N Agent  →  một nơi tiếp nhận',20,C.gray,{bold:true,align:'center'});
txt(s1,'s1-member-label',72,584,220,24,'NHÓM 5',17,C.gray,{bold:true});
txt(s1,'s1-member-a',72,608,295,28,'Võ Trọng Phúc',18,C.ink);
txt(s1,'s1-member-b',365,608,315,28,'Nguyễn Bá Quang',18,C.ink);
txt(s1,'s1-member-c',72,637,295,28,'Lý Tuyết Vy',18,C.ink);
txt(s1,'s1-member-d',365,637,315,28,'Đỗ Minh Ngọc',18,C.ink);
txt(s1,'s1-footer',960,680,250,20,'ĐỀ XUẤT  ·  1 / 9',15,C.gray,{align:'right'});
addNotes(s1,`TIME: 15 giây\nSAY: Trong phòng thực hành khoảng 40 máy, người phụ trách cần biết máy nào nên được kiểm tra trước. LabGuard là đề xuất tập trung theo dõi thiết bị và hỗ trợ xử lý sự cố. Hôm nay nhóm trình bày thiết kế, chưa phải phần mềm hoàn thiện.\nPOINT: LabGuard tổ chức việc theo dõi nhiều máy từ một giao diện quản trị.\nTRANSITION: Ta bắt đầu với một tình huống cụ thể ở MAY-03.\nTECHNICAL CAUTION: Khoảng 40 máy là quy mô thiết kế dự kiến.`);

// Move scene 2 after the newly created cover.
s2.moveTo(1);

// Slide 3 — genuine physical star and separate logical paths.
const s3=p.slides.add(); s3.background.fill=C.bg; header(s3,3,'Mỗi máy trạm kết nối về một Central','');
// Physical Ethernet spokes terminate at the LAN switch.
line(s3,'physical-agent-switch',185,300,548,383,C.gray3,4);
line(s3,'physical-central-switch',640,283,640,337,C.gray3,4);
line(s3,'physical-admin-switch',1095,300,732,383,C.gray3,4);
line(s3,'physical-sql-switch',640,495,640,433,C.gray3,4);
const agent=rect(s3,'node-agent',55,185,260,115,C.white,C.gray2,2,10);
const central=rect(s3,'node-central',510,150,260,133,C.navy,C.navy,1,10);
const admin=rect(s3,'node-admin',965,185,260,115,C.white,C.gray2,2,10);
const sql=rect(s3,'node-sql',510,495,260,100,C.white,C.gray2,2,10);
rect(s3,'node-switch',500,337,280,96,'#E5EBEA',C.gray3,2,8);
// Node labels.
txt(s3,'agent-name',70,201,230,36,'AGENT CLIENTS',22,C.ink,{bold:true,align:'center'});
txt(s3,'agent-range',70,240,230,40,'PC 01 … 40  ·  DỰ KIẾN',18,C.gray,{align:'center'});
txt(s3,'central-name',525,164,230,42,'CENTRAL SERVER',23,C.white,{bold:true,align:'center'});
txt(s3,'central-role',525,210,230,52,'TCP server\nnhận dữ liệu và quản lý',17,'#CFD9DE',{align:'center'});
txt(s3,'admin-name',980,201,230,36,'ADMIN WINFORMS',22,C.ink,{bold:true,align:'center'});
txt(s3,'admin-role',980,240,230,40,'TCP client  ·  người quản trị',18,C.gray,{align:'center'});
txt(s3,'sql-name',525,505,230,38,'SQL SERVER',22,C.ink,{bold:true,align:'center'});
txt(s3,'sql-role',525,543,230,35,'máy riêng trên LAN',18,C.gray,{align:'center'});
txt(s3,'switch-name',520,346,240,36,'SWITCH LAN',21,C.ink,{bold:true,align:'center'});
txt(s3,'switch-role',520,382,240,33,'chuyển tiếp Ethernet · Layer 2',17,C.gray,{align:'center'});
// Logical application connections between clients and Central.
for(const [name,from,to,fromSide,toSide] of [
 ['logical-agent-central',agent,central,'right','left'],
 ['logical-admin-central',admin,central,'left','right'],
]){
 const c=s3.shapes.connect(from,to,{kind:'straight',fromSide,toSide,line:{style:'dashed',fill:C.teal,width:3},head:{type:'arrow',width:'sm',length:'sm'},tail:{type:'arrow',width:'sm',length:'sm'}});c.name=name;c.bringToFront();
}
// Logical database channel uses a distinct color and route outside the switch box.
line(s3,'sql-logical-top',770,218,840,218,C.amber,3,'dashed');
line(s3,'sql-logical-side',840,218,840,545,C.amber,3,'dashed');
line(s3,'sql-logical-bottom',840,545,770,545,C.amber,3,'dashed');
txt(s3,'tcp-agent-label',315,149,190,28,'TCP 5000',18,C.teal,{bold:true,align:'center'});
txt(s3,'tcp-admin-label',770,149,190,28,'TCP 5000',18,C.teal,{bold:true,align:'center'});
txt(s3,'sql-link-label',845,405,145,60,'Central ↔ SQL\n1433 nếu cấu hình tĩnh',17,C.amber,{bold:true,align:'center'});
// Bring the physical switch card back over its connecting lines.

// Legend and responsibility statement.
line(s3,'legend-physical',64,631,120,631,C.gray3,4,'solid');txt(s3,'legend-physical-label',132,616,270,30,'Ethernet vật lý',17,C.gray);
line(s3,'legend-tcp',420,631,476,631,C.teal,3,'dashed');txt(s3,'legend-tcp-label',488,616,330,30,'Kết nối ứng dụng TCP',17,C.teal);
line(s3,'legend-sql',840,631,896,631,C.amber,3,'dashed');txt(s3,'legend-sql-label',908,616,300,30,'Kênh Central–SQL',17,C.amber);
txt(s3,'topology-authority',64,655,780,24,'Agent/Admin không truy cập SQL trực tiếp; Central là đầu mối xử lý.',17,C.ink,{bold:true});
addNotes(s3,`TIME: 45 giây\nSAY: Các máy trạm, máy chạy Admin, Central và SQL Server đều có cáp Ethernet tới switch; đây là hình sao vật lý. Nét đứt teal là phiên ứng dụng TCP: Agent và Admin là TCP client, Central là TCP server tại cổng đề xuất 5000. Central mới là thành phần truy cập SQL. Switch chỉ chuyển tiếp frame trong LAN, không xử lý nghiệp vụ. SQL có kết nối mạng riêng; cổng TCP 1433 chỉ dùng nếu được cấu hình tĩnh, không phải mặc định chung cho mọi named instance SQL Server Express.\nPOINT: Hình phân biệt cáp vật lý với các luồng TCP logic; chỉ Central truy cập cơ sở dữ liệu.\nTRANSITION: Có đường truyền rồi, ta xem một mẫu CPU được đóng thành bản tin ra sao.\nTECHNICAL CAUTION: Tên cổng và các kết nối thể hiện cấu hình đề xuất; chưa khẳng định đã triển khai LAN.`);

// Slide 4 — visible NT106 framing concept.
const s4=p.slides.add(); s4.background.fill=C.bg; header(s4,4,'Từ phép đo CPU đến một frame TCP','Một sample minh họa được đóng trong JSON rồi đóng khung ở tầng ứng dụng.');
// four-stage left-to-right story
const stageY=250;
rect(s4,'metric-panel',58,stageY,205,160,C.white,C.gray2,2,10);
txt(s4,'metric-eyebrow',74,stageY+14,173,28,'MAY-03 · MINH HỌA',16,C.teal,{bold:true,align:'center'});
txt(s4,'metric-value',74,stageY+50,173,64,'92.4%',44,C.ink,{bold:true,align:'center'});
txt(s4,'metric-label',74,stageY+116,173,28,'CPU sample',18,C.gray,{align:'center'});
arrow(s4,'arr-metric-envelope',273,316,36,26,C.teal);
rect(s4,'envelope-panel',315,stageY,225,160,'#EEF2F1',C.gray3,1,8);
txt(s4,'envelope-title',330,stageY+18,195,32,'MetricBatchPayload',19,C.ink,{bold:true,align:'center'});
line(s4,'envelope-rule',342,stageY+62,513,stageY+62,C.gray3,2);
txt(s4,'envelope-inner',331,stageY+71,193,65,'WireEnvelope\nJSON UTF-8',20,C.ink,{align:'center'});
arrow(s4,'arr-envelope-frame',549,316,36,26,C.teal);
rect(s4,'frame-head',595,stageY+32,160,94,C.teal,'none',0,8);
rect(s4,'frame-body',755,stageY+32,250,94,C.navy,'none',0,8);
txt(s4,'frame-head-label',605,stageY+41,140,75,'4 byte\nlength N\nBig-Endian',20,C.white,{bold:true,align:'center'});
txt(s4,'frame-body-label',768,stageY+45,224,68,'Payload JSON\nN ≤ 256 KiB',21,C.white,{bold:true,align:'center'});
txt(s4,'frame-caption',620,stageY+132,385,25,'[ length-prefix ][ WireEnvelope ]',17,C.gray,{align:'center'});
arrow(s4,'arr-frame-central',1012,316,36,26,C.teal);
rect(s4,'central-read-panel',1052,stageY,170,160,C.navy,'none',0,10);
txt(s4,'central-read-title',1066,stageY+19,142,34,'CENTRAL',22,C.white,{bold:true,align:'center'});
txt(s4,'central-read-copy',1066,stageY+62,142,84,'đọc đủ header\n→ đủ N byte',20,'#CFD9DE',{align:'center'});
// Byte stream principle and exact-read explanation.
rect(s4,'byte-stream-band',66,462,1148,72,'#E6EEEC','none',0,10);
txt(s4,'byte-stream-principle',88,474,1104,42,'TCP giữ thứ tự byte — không giữ ranh giới bản tin ứng dụng',25,C.ink,{bold:true,align:'center'});
txt(s4,'exact-read-copy',85,551,1110,46,'NetworkStream có thể chia hoặc ghép dữ liệu; FrameCodec lặp đọc đủ prefix và payload.',21,C.ink,{align:'center'});
txt(s4,'frame-caution',90,604,1100,34,'4 byte là length-prefix của LabGuard, không phải TCP header; header không tính trong N.',18,C.gray,{align:'center'});
addNotes(s4,`TIME: 40 giây\nSAY: Ví dụ MAY-03 tạo một mẫu CPU 92,4 phần trăm. Mẫu nằm trong MetricBatchPayload, bên trong WireEnvelope được mã hóa JSON UTF-8. LabGuard đặt trước payload một số độ dài 4 byte Big-Endian; N là số byte payload, không gồm chính header, và không vượt 256 KiB. Đây là framing do ứng dụng định nghĩa, không phải header TCP. TCP giữ thứ tự byte nhưng một lần đọc có thể chỉ trả một phần frame hoặc nhiều frame. Vì vậy FrameCodec đọc lặp cho đủ 4 byte header, giải mã N rồi đọc đủ N byte payload.\nPOINT: TCP không tạo ranh giới bản tin; ứng dụng cần framing và đọc đủ dữ liệu.\nTRANSITION: Khi Central đã nhận frame đủ, bước kế tiếp là xác thực và lưu bền vững.\nTECHNICAL CAUTION: Giá trị CPU chỉ minh họa; payload byte thực tế thay đổi theo nội dung JSON.`);

// Slide 5 — SQL transaction and acknowledgement order.
const s5=p.slides.add(); s5.background.fill=C.bg; header(s5,5,'Nhận dữ liệu chưa phải là lưu thành công','ACK chỉ được gửi khi Central xác nhận giao dịch SQL đã commit.');
pill(s5,'auth-prerequisite',65,185,392,'Điều kiện trước: phiên Agent đã xác thực',C.teal2,C.teal);
// Agent, Central and SQL flow blocks
rect(s5,'s5-agent',64,270,210,130,C.white,C.gray2,2,10);
txt(s5,'s5-agent-name',78,284,182,34,'AGENT',22,C.ink,{bold:true,align:'center'});
txt(s5,'s5-agent-action',78,326,182,60,'gửi\nMETRIC_BATCH',20,C.gray,{align:'center'});
arrow(s5,'s5-arrow-1',290,323,42,28,C.teal);
rect(s5,'s5-central',347,245,355,178,C.navy,'none',0,12);
txt(s5,'s5-central-name',367,257,315,35,'CENTRAL',22,C.white,{bold:true,align:'center'});
txt(s5,'s5-central-action',367,302,315,98,'kiểm tra phiên + dữ liệu\nkhử trùng lặp theo khóa\nDeviceId + BootId + BatchId',19,'#D6E0E4',{align:'center'});
arrow(s5,'s5-arrow-2',718,323,42,28,C.amber);
rect(s5,'s5-sql',780,245,425,178,C.white,C.amber,2,12);
txt(s5,'s5-sql-name',800,258,385,35,'GIAO DỊCH SQL DỰ KIẾN',20,C.ink,{bold:true,align:'center'});
txt(s5,'s5-sql-action',800,300,385,78,'ProcessedBatches + MetricSamples\nCOMMIT',20,C.ink,{align:'center'});
pill(s5,'commit-pill',910,378,165,'COMMIT',C.amber2,C.amber);
// Return path — central, not SQL, sends the application ACK.
line(s5,'ack-return-line',166,474,520,474,C.teal,4,'dashed');
s5.shapes.add({geometry:'leftArrow',name:'ack-return-arrow',position:{left:151,top:462,width:40,height:24},fill:C.teal,line:{style:'solid',fill:'none',width:0}});
txt(s5,'ack-label',205,429,500,32,'BATCH_ACK  ←  Central, after SQL COMMIT',20,C.teal,{bold:true,align:'center'});
// Table purpose and incident rule are separate from transport.
line(s5,'s5-divider',65,520,1210,520,C.gray2,2);
txt(s5,'s5-schema-heading',68,536,210,31,'BA BẢNG HIỆN CÓ',18,C.gray,{bold:true});
txt(s5,'s5-table-pb',68,574,330,33,'ProcessedBatches · chống lưu lặp',19,C.ink,{bold:true});
txt(s5,'s5-table-ms',422,574,370,33,'MetricSamples · CPU/RAM/Uptime hiện tại',18,C.ink,{bold:true});
txt(s5,'s5-table-inc',814,574,330,33,'Incidents · trạng thái sự cố',19,C.ink,{bold:true});
rect(s5,'s5-incident-rule',68,624,1135,38,C.amber2,'none',0,8);
txt(s5,'s5-incident-rule-text',84,627,1102,31,'Điều kiện cảnh báo dự kiến: CPU ≥ 90% trong 3 mẫu liên tiếp → HIGH_CPU',18,C.ink,{bold:true,align:'center'});
addNotes(s5,`TIME: 40 giây\nSAY: Trước khi nhận telemetry, Agent cần có phiên đã xác thực. Central kiểm tra bản tin và dùng khóa DeviceId, BootId, BatchId để nhận ra mẻ gửi lại. Theo thiết kế, Central ghi nhận batch và các mẫu MetricSamples trong giao dịch SQL. Chỉ sau COMMIT thành công Central mới trả BATCH_ACK; retry của batch đã commit không tạo bản ghi lặp và được Central trả lại ACK theo hợp đồng. Incidents là phần theo dõi sự cố riêng: quy tắc CPU từ 90 phần trăm trong ba mẫu liên tiếp là điều kiện dự kiến, không phải kết quả chạy thử.\nPOINT: ACK bền vững phụ thuộc thứ tự commit; DDL không chứng minh pipeline đã hoạt động.\nTRANSITION: Khi Central cung cấp dữ liệu có thẩm quyền, Admin có thể trình bày bằng chứng để người trực quyết định.\nTECHNICAL CAUTION: SQL persistence và ACK sau commit chưa được nghiệm thu toàn tuyến; schema hiện lưu CPU/RAM/Uptime, còn mở rộng telemetry nằm trong RFC đang chờ quyết định.`);

// Slide 6 — product UI concept with fictional, internally consistent data.
const s6=p.slides.add(); s6.background.fill=C.navy; header(s6,6,'Từ cảnh báo đến quyết định xử lý','MAY-03 · dữ liệu minh họa, không phải giao diện phần mềm đã hoàn thành.',true);
txt(s6,'s6-demo-label',695,37,520,28,'PHÁC THẢO GIAO DIỆN — DỮ LIỆU MINH HỌA',18,'#83D0C6',{bold:true,align:'right'});
rect(s6,'s6-window',60,188,1160,438,C.white,C.gray3,2,10);
rect(s6,'s6-titlebar',60,188,1160,42,'#E7EBEC','none',0,10);
rect(s6,'s6-titlebar-cover',60,216,1160,14,'#E7EBEC','none',0,0);
txt(s6,'s6-app-name',82,193,450,32,'LabGuard Admin  ·  BẢN PHÁC THẢO',19,C.ink,{bold:true});
// navigation rail
rect(s6,'s6-nav',60,230,156,396,'#E9EDED','none',0,0);
txt(s6,'s6-nav-1',80,255,118,35,'Tổng quan',18,C.gray);
rect(s6,'s6-nav-selected',60,298,156,52,C.teal2,'none',0,0);
rect(s6,'s6-nav-marker',60,298,5,52,C.teal,'none',0,0);
txt(s6,'s6-nav-2',80,304,118,38,'Thiết bị',19,C.teal,{bold:true});
txt(s6,'s6-nav-3',80,365,118,38,'Sự cố',18,C.gray);
txt(s6,'s6-nav-room',78,533,124,55,'Phòng máy\n01',17,C.gray);
// device content
const panelX=238;
txt(s6,'s6-device-title',panelX,242,500,42,'MAY-03  ·  Chi tiết thiết bị',28,C.ink,{bold:true});
pill(s6,'s6-connect',panelX,293,106,'ONLINE',C.teal2,C.teal);
pill(s6,'s6-monitor',panelX+123,293,142,'HIGH_CPU',C.amber2,C.amber);
pill(s6,'s6-workflow',panelX+282,293,104,'OPEN','#E8ECEF',C.ink);
txt(s6,'s6-connect-copy',panelX,329,150,23,'Kết nối · còn liên lạc',16,C.gray,{align:'center'});
txt(s6,'s6-monitor-copy',panelX+153,329,165,23,'Giám sát · CPU cao',16,C.gray,{align:'center'});
txt(s6,'s6-workflow-copy',panelX+324,329,176,23,'Quy trình · mới mở',16,C.gray,{align:'center'});
// metrics
const mx=[panelX,panelX+177,panelX+354];
for(let i=0;i<3;i++) rect(s6,`s6-metric-box-${i}`,mx[i],350,158,94,'#F1F4F3','none',0,7);
txt(s6,'s6-cpu-label',mx[0]+12,357,134,24,'CPU',16,C.gray,{bold:true,align:'center'});txt(s6,'s6-cpu-value',mx[0]+12,382,134,48,'92.4%',31,C.ink,{bold:true,align:'center'});
txt(s6,'s6-ram-label',mx[1]+12,357,134,24,'RAM',16,C.gray,{bold:true,align:'center'});txt(s6,'s6-ram-value',mx[1]+12,382,134,48,'58.5%',31,C.ink,{bold:true,align:'center'});
txt(s6,'s6-up-label',mx[2]+12,357,134,24,'UPTIME',16,C.gray,{bold:true,align:'center'});txt(s6,'s6-up-value',mx[2]+12,382,134,48,'12h 34m',26,C.ink,{bold:true,align:'center'});
txt(s6,'s6-history-heading',panelX,460,520,28,'CPU · ba mẫu giả định liên tiếp',18,C.gray,{bold:true});
line(s6,'s6-history-line',panelX+66,527,panelX+445,527,C.gray3,3);
const xs=[panelX+82,panelX+254,panelX+427], vals=['91.2%','93.1%','92.4%'];
for(let i=0;i<3;i++){
 rect(s6,`s6-history-dot-${i}`,xs[i]-10,517,20,20,C.teal,'none',0,10);
 txt(s6,`s6-history-value-${i}`,xs[i]-55,486,110,28,vals[i],20,C.teal,{bold:true,align:'center'});
}
txt(s6,'s6-sample-caption',panelX+8,550,500,28,'Mỗi mẫu ≥ ngưỡng minh họa 90%',17,C.gray,{align:'center'});
txt(s6,'s6-scope-caution',panelX,590,520,25,'Đĩa · mạng · nhiệt độ CPU: mục tiêu, chưa hiển thị số đo thực.',16,C.gray,{align:'center'});
// incident panel
rect(s6,'s6-incident-panel',786,242,407,364,'#F7F2E9',C.amber,1,12);
txt(s6,'s6-incident-eyebrow',808,256,364,28,'PHIẾU SỰ CỐ · MINH HỌA',17,C.amber,{bold:true});
txt(s6,'s6-incident-title',808,288,364,36,'HIGH_CPU',25,C.ink,{bold:true});
txt(s6,'s6-evidence-title',808,334,364,28,'BẰNG CHỨNG',17,C.gray,{bold:true});
txt(s6,'s6-evidence',808,361,364,46,'91.2% → 93.1% → 92.4%\nBa mẫu liên tiếp vượt 90%',19,C.ink,{bold:true});
line(s6,'s6-panel-rule',808,416,1172,416,C.gray2,2);
txt(s6,'s6-guidance-title',808,426,364,27,'HƯỚNG KIỂM TRA',17,C.gray,{bold:true});
txt(s6,'s6-guidance',808,454,364,68,'1  Xem tiến trình dùng CPU cao.\n2  Xác minh ảnh hưởng buổi học.\n3  Đối chiếu mẫu mới sau kiểm tra.',18,C.ink);
rect(s6,'s6-human-decision',808,538,364,49,C.navy,'none',0,7);
txt(s6,'s6-human-decision-text',823,544,334,37,'Người quản trị quyết định thao tác.',18,C.white,{bold:true,align:'center'});
addNotes(s6,`TIME: 55 giây\nSAY: Đây là phác thảo giao diện, toàn bộ giá trị đều là dữ liệu minh họa. Ba trạng thái có nghĩa khác nhau: ONLINE nói Agent còn liên lạc; HIGH_CPU là điều kiện giám sát; OPEN là quy trình sự cố mới mở. Ba mẫu 91,2, 93,1 và 92,4 phần trăm đều vượt ngưỡng minh họa 90; RAM 58,5 phần trăm và uptime 12 giờ 34 phút là ví dụ nhất quán. Phần hỗ trợ đưa ra bằng chứng và gợi ý kiểm tra, như xem tiến trình chiếm CPU và xác minh ảnh hưởng lớp. Người quản trị có thẩm quyền mới quyết định xử lý.\nPOINT: Bằng chứng dẫn tới bước kiểm tra; cảnh báo không chứng minh máy hỏng và không tự sửa máy.\nTRANSITION: Đây là trải nghiệm dự kiến; ta phân biệt nó với những thành phần thực sự đã có.\nTECHNICAL CAUTION: Admin chưa có giao diện và binding này. Mục tiêu sản phẩm còn có đĩa, mạng, uptime và nhiệt độ CPU best-effort khi phần cứng hỗ trợ; phạm vi hợp đồng mở rộng còn nằm trong RFC-001 đang chờ quyết định.`);

// Slide 7 — status grounded in current GitHub main WBS.
const s7=p.slides.add(); s7.background.fill=C.bg; header(s7,7,'Đã có nền tảng — còn thiếu tích hợp toàn tuyến','Trạng thái WBS trên main mới nhất: 40 nhiệm vụ.');
// status count bar
const barX=70,barY=204,barW=1138,barH=34;rect(s7,'status-bar-done',barX,barY,barW*6/40,barH,C.teal,'none',0,8);rect(s7,'status-bar-progress',barX+barW*6/40,barY,barW*2/40,barH,C.amber,'none',0,0);rect(s7,'status-bar-not-started',barX+barW*8/40,barY,barW*32/40,barH,'#D9DEE0','none',0,8);
txt(s7,'status-done-count',70,248,250,48,'6  ĐÃ XONG',25,C.teal,{bold:true});txt(s7,'status-progress-count',421,248,310,48,'2  ĐANG LÀM',25,C.amber,{bold:true,align:'center'});txt(s7,'status-not-started-count',850,248,350,48,'32  CHƯA LÀM',25,C.gray,{bold:true,align:'right'});
// Columns
rect(s7,'status-col-done',65,320,348,255,C.white,C.gray2,1,10);rect(s7,'status-col-progress',466,320,348,255,C.white,C.gray2,1,10);rect(s7,'status-col-pending',867,320,348,255,C.white,C.gray2,1,10);
txt(s7,'status-done-title',84,337,309,33,'ĐÃ CÓ  ·  Ở MỨC THÀNH PHẦN',18,C.teal,{bold:true});
txt(s7,'status-done-body',84,380,306,170,'Protocol, DTO, FrameCodec\nvà kiểm thử.\n\nAgent có các thành phần\nCPU/RAM/Uptime.\n\nCentral TCP listener; TcpEcho.',19,C.ink);
txt(s7,'status-progress-title',486,337,308,33,'ĐANG TRIỂN KHAI',18,C.amber,{bold:true});
txt(s7,'status-progress-body',486,380,306,170,'B-005 · SQL persistence\nchưa nghiệm thu.\n\nC-001 · thiết kế lại Admin\nchưa có giao diện hoàn thiện.',19,C.ink);
txt(s7,'status-pending-title',887,337,308,33,'CHƯA HOÀN TẤT / CHƯA XÁC MINH',17,C.gray,{bold:true});
txt(s7,'status-pending-body',887,380,306,170,'Phiên/xác thực · SQL/ACK\nReconnect/ReplayBuffer\nQuy trình sự cố · Admin binding\nGiám sát đầu-cuối Agent → Admin',18,C.ink);
txt(s7,'status-tests',70,602,1130,34,'195 bài đạt trong lượt kiểm tra Windows được ghi nhận tại PR #11 · local, không phải CI hay kiểm thử đầu-cuối.',17,C.gray,{align:'center'});
txt(s7,'status-rfc',70,642,1130,27,'RFC mở rộng telemetry/incident là bản nháp; chưa phê duyệt hợp đồng hoặc schema.',17,C.ink,{bold:true,align:'center'});
addNotes(s7,`TIME: 35 giây\nSAY: WBS trên main có 40 nhiệm vụ: 6 xong, 2 đang làm và 32 chưa làm. Phần xong là các thành phần như protocol, framing, bộ thu CPU/RAM/Uptime, Central TCP listener và TcpEcho; điều đó không có nghĩa monitoring toàn tuyến đã chạy. B-005 persistence và C-001 giao diện Admin còn đang làm. Xác thực, SQL commit rồi ACK, reconnect/replay, quy trình sự cố và binding Admin chưa hoàn tất hoặc chưa được xác minh đầu-cuối. Mốc 195 bài đạt là lượt chạy cục bộ Windows ghi trong PR #11, không phải GitHub CI.\nPOINT: Tiến độ thật ở mức thành phần; chưa có bằng chứng Agent → Central → SQL → Admin vận hành toàn tuyến.\nTRANSITION: Các phụ thuộc còn lại quyết định thứ tự phát triển toàn đồ án.\nTECHNICAL CAUTION: Hai RFC hiện là bản nháp; merge tài liệu không phê duyệt thay đổi giao thức, schema hay tính năng.`);

// Slide 8 — whole-project relative roadmap and official owners.
const s8=p.slides.add(); s8.background.fill=C.bg; header(s8,8,'Từ kết nối TCP đến giám sát tập trung','Lộ trình toàn đồ án — các giai đoạn chưa hoàn tất.');
const phaseXs=[63,357,651,945], phaseW=270;
const phases=[
 ['01','KẾT NỐI','Protocol · TCP\nPhiên và xác thực'],
 ['02','THU THẬP & LƯU','Telemetry · SQL\nGiao dịch · BATCH_ACK'],
 ['03','PHÁT HIỆN & PHỤC HỒI','Liveness · sự cố\nReconnect · replay'],
 ['04','QUẢN TRỊ & TÍCH HỢP','Admin · kiểm thử TCP\nHỗ trợ sự cố trong Admin'],
];
for(let i=0;i<4;i++){
 const x=phaseXs[i]; rect(s8,`roadmap-node-${i}`,x,190,phaseW,170,i===0?C.navy:C.white,i===0?C.navy:C.gray2,2,10);
 txt(s8,`roadmap-num-${i}`,x+18,202,48,35,phases[i][0],22,i===0?C.teal:C.teal,{bold:true});
 txt(s8,`roadmap-title-${i}`,x+18,242,phaseW-36,38,phases[i][1],19,i===0?C.white:C.ink,{bold:true});
 txt(s8,`roadmap-body-${i}`,x+18,286,phaseW-36,64,phases[i][2],18,i===0?'#C4D2D8':C.gray,{align:'left'});
 if(i<3) arrow(s8,`roadmap-arrow-${i}`,x+phaseW+6,260,25,22,C.teal);
}
// Official owner mapping in a clean two-column footer.
line(s8,'roadmap-owner-rule',64,388,1215,388,C.gray2,2);
txt(s8,'owner-a',70,404,540,43,'Võ Trọng Phúc — Protocol & Agent',19,C.ink,{bold:true});
txt(s8,'owner-b',658,404,550,43,'Nguyễn Bá Quang — Central Server & SQL Server',19,C.ink,{bold:true});
txt(s8,'owner-c',70,451,540,43,'Lý Tuyết Vy — Admin WinForms',19,C.ink,{bold:true});
txt(s8,'owner-d',658,451,550,52,'Đỗ Minh Ngọc — Công cụ kiểm thử truyền thông TCP\nvà hỗ trợ phân tích, xử lý sự cố tích hợp trong LabGuard',18,C.ink,{bold:true});
rect(s8,'acceptance-target',66,526,1145,108,C.navy,'none',0,10);
txt(s8,'acceptance-label',88,538,1090,28,'MỤC TIÊU TÍCH HỢP ƯU TIÊN',17,'#83D0C6',{bold:true});
txt(s8,'acceptance-flow',88,570,1090,48,'Agent xác thực → METRIC_BATCH → SQL COMMIT → BATCH_ACK → Admin nhận trạng thái từ Central',20,C.white,{bold:true,align:'center'});
addNotes(s8,`TIME: 25 giây\nSAY: Nhóm dự kiến làm theo phụ thuộc: trước hết hoàn thiện protocol, TCP và phiên; sau đó nối telemetry với giao dịch SQL và ACK; tiếp đến liveness, sự cố và phục hồi; cuối cùng tích hợp Admin, công cụ kiểm thử TCP và phần hỗ trợ phân tích trong Admin. Phúc làm Protocol/Agent, Quang Central/SQL, Vy Admin, Ngọc công cụ TCP và hỗ trợ xử lý sự cố. Mục tiêu ưu tiên là xác thực, ghi SQL bền vững, ACK rồi đưa trạng thái có thẩm quyền tới Admin.\nPOINT: Đây là lộ trình phát triển toàn đồ án, không phải cam kết hoàn tất vào buổi giữa kỳ.\nTRANSITION: Khi hoàn thành theo thứ tự này, LabGuard mới có thể hỗ trợ theo dõi tập trung dựa trên dữ liệu đã kiểm chứng.\nTECHNICAL CAUTION: Các giai đoạn là kế hoạch tương đối, không phải mốc ngày hoặc tuyên bố đã hoàn thành.`);

// Slide 9 — concise synthesis and invitation for feedback.
const s9=p.slides.add(); s9.background.fill=C.bg; header(s9,9,'LabGuard — giám sát tập trung, xử lý có căn cứ','Đề xuất thiết kế cho đồ án NT106.');
const sumX=[78,467,856];
const sums=[['01','TẬP TRUNG','Nhiều Agent gửi dữ liệu\nqua Central.'],['02','TRUYỀN TIN CÓ KHUNG','TCP byte stream; length-prefix\ngiúp đọc đúng bản tin.'],['03','QUYẾT ĐỊNH CÓ CĂN CỨ','Xem bằng chứng, đề xuất\nbước kiểm tra; người quyết định.']];
for(let i=0;i<3;i++){
 txt(s9,`s9-num-${i}`,sumX[i],240,300,50,sums[i][0],36,C.teal,{bold:true});
 txt(s9,`s9-title-${i}`,sumX[i],302,340,42,sums[i][1],22,C.ink,{bold:true});
 txt(s9,`s9-copy-${i}`,sumX[i],354,342,82,sums[i][2],21,C.gray);
}
line(s9,'s9-rule',78,471,1205,471,C.gray2,2);
txt(s9,'s9-proposal',80,502,1120,42,'Thiết kế đang được đề xuất; tích hợp toàn tuyến chưa nghiệm thu.',22,C.ink,{bold:true,align:'center'});
txt(s9,'s9-feedback',80,558,1120,48,'Nhóm 5 xin nhận góp ý về phạm vi và thứ tự tích hợp.',24,C.teal,{bold:true,align:'center'});
addNotes(s9,`TIME: 15 giây\nSAY: LabGuard hướng tới theo dõi tập trung, truyền nhận bản tin TCP có framing rõ ràng và hỗ trợ người quản trị xử lý dựa trên bằng chứng. Hiện đây vẫn là đề xuất với tiến độ thành phần như vừa trình bày; nhóm xin nhận góp ý về phạm vi và thứ tự tích hợp.\nPOINT: Mục tiêu là tổ chức dữ liệu và quyết định của người quản trị, không tự sửa máy.\nTRANSITION: Kết thúc phần trình bày; nhóm sẵn sàng trao đổi và ghi nhận góp ý.\nTECHNICAL CAUTION: Không có kết quả triển khai toàn tuyến hoặc đo kiểm thực nghiệm được trình bày.`);

if(p.slides.items.length!==9) throw new Error(`Expected exactly 9 slides, got ${p.slides.items.length}`);
// Render all slides at 2560x1440 for review and export final files.
for(let i=0;i<p.slides.items.length;i++){
 const blob=await p.export({slide:p.slides.items[i],format:'png',scale:2});
 await fs.writeFile(`${out}LabGuard_Nhom5_GK_Slide_${String(i+1).padStart(2,'0')}.png`,new Uint8Array(await blob.arrayBuffer()));
}
const deck=await PresentationFile.exportPptx(p); await deck.save(`${out}LabGuard_Nhom5_GK_Keynote_REWORK.pptx`);
console.log(JSON.stringify({slides:p.slides.items.length,out,deckBytes:(await fs.stat(`${out}LabGuard_Nhom5_GK_Keynote_REWORK.pptx`)).size}));
