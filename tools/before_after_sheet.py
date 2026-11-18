import sys, os
from PIL import Image, ImageDraw, ImageFont
A,B=sys.argv[1],sys.argv[2]; NAME=sys.argv[3] if len(sys.argv)>3 else B; S='/workspace/shots/eastside-build/stage2'
f=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',30); g=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',22)
w,h=960,540; pad=12; lab=36
tour=['start.png']+[f'keys-{i}-Digit{i+1}.png' for i in range(1,8)]
tn=['Avenue','Signal crossing','Open lot','Block from above','Backyards','Stop sign crossing','District','Parked cars']
cl=['start.png']+[f'keys-{i}-Digit{(i+1)%10}.png' for i in range(1,10)]
cn=['Palm fronds (striping)','Barbershop wall','White house, red roof','Windows at dusk','Hoodie (side blotch)','Unc talk pose and the Unc prompt','Objective marker','Yard (grass clumps cut in v74)','Wood fence planks','Windows at 21:00']
hud=(1500,0,1920,300); prm=(0,240,700,560); bot=(560,840,1360,1000)
sections=[]
sections.append(('Tour, Ultra',[(n,(f'{S}/tour{A}/{t}',None),(f'{S}/tour{B}/{t}',None)) for n,t in zip(tn,tour)]))
sections.append(('Close ups, 1920 x 1080 capture',[(n,(f'{S}/close{A}hr/{t}',None),(f'{S}/close{B}/{t}',None)) for n,t in zip(cn,cl)]))
u='keys-5-Digit6.png'
sections.append(('HUD text and prompts, zoomed crops of the Unc view',[
 ('HUD: FISTS, money, Rep (ghosting)',(f'{S}/close{A}hr/{u}',hud),(f'{S}/close{B}/{u}',hud)),
 ('Talk to Unc prompt (left label, one prompt)',(f'{S}/close{A}hr/{u}',prm),(f'{S}/close{B}/{u}',prm)),
 (f'v{B} next to Unc (shot=close2 view 1) | v{B} Arm guards prompt on a held block',(f'{S}/close2_{B}/keys-1-Digit1.png',None),(f'{S}/close2_{B}/keys-3-Digit3.png',None))]))
sections.append(('Lighting: post profile, lower warmer sun, fog pushed out, character fill',[
 ('Avenue at eye height',(f'{S}/tour{A}/start.png',None),(f'{S}/tour{B}/start.png',None)),
 (f'Player on the road: v{A} hoodie view | v{B} shot=close2 view 2',(f'{S}/close{A}hr/keys-4-Digit5.png',None),(f'{S}/close2_{B}/keys-2-Digit2.png',None))]))
sections.append(('Night 21:00: darker ambient plus lamp pools',[
 ('Avenue at 21:00',(f'{S}/night{A}/start.png',None),(f'{S}/night{B}/start.png',None)),
 ('Block from above at 21:00',(f'{S}/night{A}/keys-1-Digit4.png',None),(f'{S}/night{B}/keys-1-Digit4.png',None)),
 (f'Stop sign crossing at 21:00 | v{B} lamp pool close up',(f'{S}/night{A}/keys-2-Digit6.png',None),(f'{S}/close2_{B}/keys-4-Digit4.png',None))]))
sections.append(('Fight feedback: target ring, warning rings, hit numbers, sparks, dust, shockwave',[
 (f'v{A} opening scuffle | v{B} shot=fight',(f'{S}/mugger{A}/keys-1-Digit9.png',None),(f'{S}/fight{B}/keys-2-Digit9.png',None)),
 (f'v{A} opening scuffle | v{B} shot=fight',(f'{S}/mugger{A}/keys-2-Digit9.png',None),(f'{S}/fight{B}/keys-3-Digit9.png',None)),
 (f'v{A} opening scuffle | v{B} shot=fight',(f'{S}/mugger{A}/keys-3-Digit9.png',None),(f'{S}/fight{B}/keys-4-Digit9.png',None))]))
sections.append(('District edge: skyline in the sky',[
 ('District from above',(f'{S}/tour{A}/keys-6-Digit7.png',None),(f'{S}/tour{B}/keys-6-Digit7.png',None)),
 (f'v{A} district | v{B} skyline view (shot=close2 view 6)',(f'{S}/tour{A}/keys-6-Digit7.png',None),(f'{S}/close2_{B}/keys-6-Digit6.png',None))]))
sections.append((f'GFX Ultra (left) vs High (right), v{B}',[
 ('Avenue',(f'{S}/tour{B}/start.png',None),(f'{S}/tour{B}high/start.png',None)),
 ('Player on the road',(f'{S}/close2_{B}/keys-2-Digit2.png',None),(f'{S}/close2high_{B}/keys-1-Digit2.png',None)),
 ('Night avenue',(f'{S}/close2_{B}/keys-5-Digit5.png',None),(f'{S}/close2high_{B}/keys-2-Digit5.png',None)),
 ('Skyline',(f'{S}/close2_{B}/keys-6-Digit6.png',None),(f'{S}/close2high_{B}/keys-3-Digit6.png',None))]))
H=70+sum(60+len(r)*(h+lab+pad) for _,r in sections)
out=Image.new('RGB',(2*w+3*pad,H),(18,16,14)); d=ImageDraw.Draw(out)
d.text((pad,18),f'Before: v{A} (left) vs after: v{NAME} work, build {B} (right). Ultra tier unless marked.',fill=(255,204,51),font=f)
def tile(spec,x,y):
    p,crop=spec
    if not os.path.exists(p): d.text((x+20,y+20),'missing '+p.replace(S+'/',''),fill=(200,80,80),font=g); return
    im=Image.open(p).convert('RGB')
    if crop:
        sx=im.size[0]/1920; im=im.crop(tuple(int(c*sx) for c in crop))
        r=min(w/im.size[0],h/im.size[1]); im=im.resize((int(im.size[0]*r),int(im.size[1]*r)),Image.LANCZOS)
        out.paste(im,(x+(w-im.size[0])//2,y+(h-im.size[1])//2))
    else: out.paste(im.resize((w,h),Image.LANCZOS),(x,y))
y=70
for title,rows in sections:
    d.text((pad,y+14),title,fill=(255,204,51),font=f); y+=60
    for t,a,b in rows:
        la,lb=(t.split(' | ')+[None])[:2] if ' | ' in t else (t+f'  (v{A})',t+f'  (v{B})')
        d.text((pad,y+6),la,fill=(230,230,230),font=g); d.text((2*pad+w,y+6),lb,fill=(230,230,230),font=g)
        tile(a,pad,y+lab); tile(b,2*pad+w,y+lab); y+=h+lab+pad
o=f'/workspace/shots/vibe/before-after-v{A}-v{NAME}.jpg'; out.save(o,quality=88); print(o,out.size)
