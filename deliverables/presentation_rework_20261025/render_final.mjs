import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const artifactToolModule = process.env.LABGUARD_ARTIFACT_TOOL_MODULE || 'file:///C:/Users/lyle3/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const { FileBlob, PresentationFile } = await import(artifactToolModule);
const dir=path.dirname(fileURLToPath(import.meta.url))+path.sep;
const p=await PresentationFile.importPptx(await FileBlob.load(`${dir}LabGuard_Nhom5_GK_Keynote_REWORK.pptx`));
if(p.slides.items.length!==9)throw new Error(`Expected 9 slides, got ${p.slides.items.length}`);
for(let i=0;i<9;i++){
 const slide=p.slides.items[i];
 const note=slide.speakerNotes.text;
 if(typeof note!=='string'||!note.includes('TIME:')||!note.includes('SAY:')||!note.includes('POINT:')||!note.includes('TRANSITION:'))throw new Error(`Missing speaker-note fields on slide ${i+1}`);
 const png=await p.export({slide,format:'png',scale:2});
 await fs.writeFile(`${dir}LabGuard_Nhom5_GK_Slide_${String(i+1).padStart(2,'0')}.png`,new Uint8Array(await png.arrayBuffer()));
}
console.log(JSON.stringify({reopened:true,slides:p.slides.items.length,notes:9,rendered:'2560x1440'}));
