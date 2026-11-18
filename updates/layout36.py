from pathlib import Path
p=Path('src35/index.html')
s=p.read_text(encoding='utf-8').replace('V3.5','V3.6')
css="""html,body{height:100%;overflow:hidden}body{display:flex;flex-direction:column}main{flex:1;min-height:0;overflow:hidden;display:grid!important;grid-template-columns:minmax(280px,360px) minmax(0,1fr)!important;align-items:stretch!important}main>aside{height:100%;min-height:0;max-height:none!important;overflow-y:auto;box-sizing:border-box;position:static!important}main>section{min-height:0;overflow:auto;position:static!important;align-self:stretch!important}details>summary{padding:8px 4px;cursor:pointer}#layoutAccordion>summary{background:#263b56;color:white;border-radius:6px;padding:12px;user-select:none;font-weight:bold}#layoutAccordion{margin:8px 0}canvas{max-width:100%;height:auto}"""
js="""document.addEventListener('DOMContentLoaded',()=>{
const sidebar=document.querySelector('main aside');if(!sidebar)return;
const title=[...document.querySelectorAll('h2,h3,h4,legend,b,strong,summary,label')].find(e=>/layout.editor|textfelder ziehen/i.test(e.textContent.trim()));
if(title){
const wrapper=document.createElement('details');wrapper.id='layoutAccordion';wrapper.open=false;
const summary=document.createElement('summary');summary.textContent='Layout-Editor · Textfelder ziehen';wrapper.append(summary);
const group=title.closest('fieldset,details,.panel,.card');
if(group&&group!==sidebar&&!group.contains(sidebar)&&group.querySelectorAll('canvas').length===0){wrapper.append(group)}else{
let next=title.nextElementSibling;const nodes=[];while(next&&!next.matches('h2,h3,h4,section,details')&&!next.querySelector('canvas')&&!next.matches('canvas')){nodes.push(next);next=next.nextElementSibling;}
nodes.forEach(n=>wrapper.append(n));
}
if(!wrapper.contains(title))title.hidden=true;
sidebar.append(wrapper);
}
});"""
s=s.replace('</style>',css+'</style>',1).replace('</body>','<script>'+js+'</script></body>',1)
p.write_text(s,encoding='utf-8')
for name in ['Program.cs','SEV.csproj','AutoUpdate.cs']:
 p=Path('src35')/name
 s=p.read_text(encoding='utf-8').replace('Generator 3.5','Generator 3.6').replace('3.5.0','3.6.0').replace('new Version(3,5,0)','new Version(3,6,0)')
 p.write_text(s,encoding='utf-8')
