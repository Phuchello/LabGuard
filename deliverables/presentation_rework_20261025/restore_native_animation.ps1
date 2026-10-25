param(
 [string]$SourcePptx = (Join-Path $PSScriptRoot 'LabGuard_Nhom5_GK_Keynote_SOURCE.pptx'),
 [string]$TargetPptx = (Join-Path $PSScriptRoot 'LabGuard_Nhom5_GK_Keynote_REWORK.pptx')
)
Add-Type -AssemblyName System.IO.Compression.FileSystem
$nsUri='http://schemas.openxmlformats.org/presentationml/2006/main'
function Read-XmlEntry($zip,$name){
 $entry=$zip.GetEntry($name); if(-not $entry){throw "Missing $name"}
 $reader=[IO.StreamReader]::new($entry.Open(),[Text.Encoding]::UTF8); try {$content=$reader.ReadToEnd()} finally {$reader.Dispose()}
 $doc=[Xml.XmlDocument]::new(); $doc.PreserveWhitespace=$true; $doc.LoadXml($content); return $doc
}
$sourceZip=[IO.Compression.ZipFile]::OpenRead($SourcePptx)
try {$source=Read-XmlEntry $sourceZip 'ppt/slides/slide4.xml'} finally {$sourceZip.Dispose()}
$sourceNs=[Xml.XmlNamespaceManager]::new($source.NameTable);$sourceNs.AddNamespace('p',$nsUri)
$transition=$source.SelectSingleNode('/p:sld/p:transition',$sourceNs)
$timing=$source.SelectSingleNode('/p:sld/p:timing',$sourceNs)
if(-not $transition -or -not $timing){throw 'Source room scene has no native transition/timing.'}
$targetZip=[IO.Compression.ZipFile]::Open($TargetPptx,[IO.Compression.ZipArchiveMode]::Update)
try {
 $target=Read-XmlEntry $targetZip 'ppt/slides/slide2.xml'
 $targetNs=[Xml.XmlNamespaceManager]::new($target.NameTable);$targetNs.AddNamespace('p',$nsUri)
 $targetIds=@($target.SelectNodes('//p:cNvPr',$targetNs)|ForEach-Object{$_.GetAttribute('id')})
 $targetable=@($timing.SelectNodes('.//p:spTgt',$sourceNs)|ForEach-Object{$_.GetAttribute('spid')}|Select-Object -Unique)
 $missing=@($targetable|Where-Object{$_ -notin $targetIds})
 if($missing.Count){throw "Animation references shape IDs absent from target slide 2: $($missing -join ', ')"}
 foreach($node in @($target.SelectNodes('/p:sld/p:transition | /p:sld/p:timing',$targetNs))){$node.ParentNode.RemoveChild($node)|Out-Null}
 $anchor=$target.SelectSingleNode('/p:sld/p:clrMapOvr',$targetNs);if(-not $anchor){$anchor=$target.SelectSingleNode('/p:sld/p:cSld',$targetNs)}
 $t=$target.ImportNode($transition,$true);$tm=$target.ImportNode($timing,$true)
 $anchor.ParentNode.InsertAfter($t,$anchor)|Out-Null;$t.ParentNode.InsertAfter($tm,$t)|Out-Null
 $entry=$targetZip.GetEntry('ppt/slides/slide2.xml');$entry.Delete()
 $new=$targetZip.CreateEntry('ppt/slides/slide2.xml',[IO.Compression.CompressionLevel]::Optimal)
 $memory=[IO.MemoryStream]::new();$settings=[Xml.XmlWriterSettings]::new();$settings.Encoding=[Text.UTF8Encoding]::new($false);$settings.OmitXmlDeclaration=$false;$settings.Indent=$false
 $writer=[Xml.XmlWriter]::Create($memory,$settings);try{$target.Save($writer)}finally{$writer.Dispose()}
 $stream=$new.Open();try{$memory.Position=0;$memory.CopyTo($stream)}finally{$stream.Dispose();$memory.Dispose()}
 $final=Read-XmlEntry $targetZip 'ppt/slides/slide2.xml'
 $finalNs=[Xml.XmlNamespaceManager]::new($final.NameTable);$finalNs.AddNamespace('p',$nsUri)
 $effectCount=@($final.SelectNodes('//p:timing//p:animEffect',$finalNs)).Count
 $motionCount=@($final.SelectNodes('//p:timing//p:animMotion',$finalNs)).Count
 $clickCount=@($final.SelectNodes('//p:timing//p:cTn[@nodeType="clickEffect"]',$finalNs)).Count
 $transitionName=$final.SelectSingleNode('/p:sld/p:transition/*',$finalNs).LocalName
 "Slide 2 native timing restored: effects=$effectCount, motionPaths=$motionCount, clickSequenceNodes=$clickCount, transition=$transitionName; target IDs validated=$($targetable.Count)."
} finally {$targetZip.Dispose()}
