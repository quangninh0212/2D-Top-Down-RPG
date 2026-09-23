# Builds the course report as a .docx by writing the Office Open XML parts and
# zipping them. Done by hand because this machine has no Word, no LibreOffice
# and no Node or Python to drive a library.
#
#   .\make-docx.ps1 -Content content.txt -Out ..\..\report.docx

param(
    [string]$Content = "$PSScriptRoot\content.txt",
    [string]$Shots = 'D:\Projects\2D-Top-Down-RPG\Docs\Screenshots',
    [string]$Out = 'D:\Projects\2D-Top-Down-RPG\BAO_CAO_DO_AN_SOULBOUND_GATE.docx'
)

$ErrorActionPreference = 'Stop'

$work = Join-Path $PSScriptRoot 'package'
if (Test-Path $work) { Remove-Item $work -Recurse -Force }

New-Item -ItemType Directory -Path $work | Out-Null
New-Item -ItemType Directory -Path "$work\_rels" | Out-Null
New-Item -ItemType Directory -Path "$work\docProps" | Out-Null
New-Item -ItemType Directory -Path "$work\word" | Out-Null
New-Item -ItemType Directory -Path "$work\word\_rels" | Out-Null
New-Item -ItemType Directory -Path "$work\word\media" | Out-Null

function Write-Part($path, $text) {
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($path, $text, $utf8)
}

function Esc($text) {
    return ($text -replace '&', '&amp;' -replace '<', '&lt;' -replace '>', '&gt;')
}

# ----- page geometry (A4, margins 3-2-2-2 cm) ---------------------------

$pageW = 11906
$pageH = 16838
$marginLeft = 1701
$marginOther = 1134
$contentWidth = $pageW - $marginLeft - $marginOther      # 9071 twips

# ----- runs and paragraphs ----------------------------------------------

function RunXml($text, $bold = $false, $italic = $false, $size = 26) {
    $props = "<w:rFonts w:ascii='Times New Roman' w:hAnsi='Times New Roman' w:cs='Times New Roman'/>"
    if ($bold) { $props += '<w:b/>' }
    if ($italic) { $props += '<w:i/>' }
    $props += "<w:sz w:val='$size'/><w:szCs w:val='$size'/>"

    return "<w:r><w:rPr>$props</w:rPr><w:t xml:space='preserve'>$(Esc $text)</w:t></w:r>"
}

function ParaXml($text, $style = $null, $align = 'both', $bold = $false, $italic = $false, $size = 26, $spaceAfter = 120) {
    $props = ''
    if ($style) { $props += "<w:pStyle w:val='$style'/>" }
    $props += "<w:spacing w:line='360' w:lineRule='auto' w:after='$spaceAfter'/>"
    $props += "<w:jc w:val='$align'/>"

    return "<w:p><w:pPr>$props</w:pPr>$(RunXml $text $bold $italic $size)</w:p>"
}

function BulletXml($text) {
    $props = "<w:pStyle w:val='ListParagraph'/><w:numPr><w:ilvl w:val='0'/><w:numId w:val='1'/></w:numPr>"
    $props += "<w:spacing w:line='360' w:lineRule='auto' w:after='60'/><w:jc w:val='both'/>"

    return "<w:p><w:pPr>$props</w:pPr>$(RunXml $text)</w:p>"
}

function HeadingXml($level, $text) {
    $size = if ($level -eq 1) { 30 } else { 28 }
    $before = if ($level -eq 1) { 0 } else { 240 }
    $break = if ($level -eq 1) { '<w:pageBreakBefore/>' } else { '' }
    $align = if ($level -eq 1) { 'center' } else { 'left' }

    $props = "<w:pStyle w:val='Heading$level'/>$break<w:spacing w:before='$before' w:after='180' w:line='360' w:lineRule='auto'/><w:jc w:val='$align'/>"

    return "<w:p><w:pPr>$props</w:pPr>$(RunXml $text $true $false $size)</w:p>"
}

# A caption that Word can collect into a list of figures or tables: the number
# comes from a SEQ field so it renumbers itself if anything is inserted.
function CaptionXml($kind, $seq, $text) {
    $props = "<w:pStyle w:val='Caption'/><w:spacing w:before='60' w:after='240' w:line='276' w:lineRule='auto'/><w:jc w:val='center'/>"

    $xml = "<w:p><w:pPr>$props</w:pPr>"
    $xml += RunXml "$kind " $false $true 24
    $xml += "<w:fldSimple w:instr=' SEQ $seq \* ARABIC '>" + (RunXml '1' $false $true 24) + '</w:fldSimple>'
    $xml += RunXml ": $text" $false $true 24
    $xml += '</w:p>'

    return $xml
}

# ----- images -----------------------------------------------------------

$script:imageRels = @()
$script:imageIndex = 0

function ImageXml($fileName) {
    $source = Join-Path $Shots $fileName
    if (-not (Test-Path $source)) { throw "Missing image: $source" }

    $script:imageIndex++
    $id = $script:imageIndex
    $relId = "rIdImg$id"
    $target = "media/image$id.png"

    Copy-Item $source (Join-Path $work "word\media\image$id.png")
    $script:imageRels += "<Relationship Id='$relId' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/image' Target='$target'/>"

    # PNG stores its size as two big-endian 32-bit numbers at offset 16.
    $bytes = [System.IO.File]::ReadAllBytes($source)
    $pxW = [int]$bytes[16] * 16777216 + [int]$bytes[17] * 65536 + [int]$bytes[18] * 256 + [int]$bytes[19]
    $pxH = [int]$bytes[20] * 16777216 + [int]$bytes[21] * 65536 + [int]$bytes[22] * 256 + [int]$bytes[23]

    # 15.5 cm wide, keeping the aspect ratio. 360000 EMU to the centimetre.
    $cx = [int](15.5 * 360000)
    $cy = [int]($cx * $pxH / $pxW)

    $xml = "<w:p><w:pPr><w:spacing w:before='180' w:after='60'/><w:jc w:val='center'/></w:pPr><w:r><w:drawing>"
    $xml += "<wp:inline distT='0' distB='0' distL='0' distR='0'>"
    $xml += "<wp:extent cx='$cx' cy='$cy'/><wp:effectExtent l='0' t='0' r='0' b='0'/>"
    $xml += "<wp:docPr id='$id' name='Picture $id'/>"
    $xml += "<wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect='1'/></wp:cNvGraphicFramePr>"
    $xml += "<a:graphic><a:graphicData uri='http://schemas.openxmlformats.org/drawingml/2006/picture'>"
    $xml += "<pic:pic><pic:nvPicPr><pic:cNvPr id='$id' name='image$id.png'/><pic:cNvPicPr/></pic:nvPicPr>"
    $xml += "<pic:blipFill><a:blip r:embed='$relId'/><a:stretch><a:fillRect/></a:stretch></pic:blipFill>"
    $xml += "<pic:spPr><a:xfrm><a:off x='0' y='0'/><a:ext cx='$cx' cy='$cy'/></a:xfrm>"
    $xml += "<a:prstGeom prst='rect'><a:avLst/></a:prstGeom></pic:spPr></pic:pic>"
    $xml += '</a:graphicData></a:graphic></wp:inline></w:drawing></w:r></w:p>'

    return $xml
}

# ----- tables -----------------------------------------------------------

function CellXml($text, $width, $header) {
    $shading = if ($header) { "<w:shd w:val='clear' w:color='auto' w:fill='E8E4D8'/>" } else { '' }

    $xml = "<w:tc><w:tcPr><w:tcW w:w='$width' w:type='dxa'/>$shading<w:vAlign w:val='center'/></w:tcPr>"
    $xml += "<w:p><w:pPr><w:spacing w:line='240' w:lineRule='auto' w:after='40'/><w:jc w:val='left'/></w:pPr>"
    $xml += RunXml $text $header $false 24
    $xml += '</w:p></w:tc>'

    return $xml
}

function TableXml($weights, $header, $rows) {
    $widths = @()
    foreach ($weight in $weights) { $widths += [int]($contentWidth * $weight) }

    $sum = ($widths | Measure-Object -Sum).Sum
    $widths[0] += $contentWidth - $sum

    $grid = ''
    foreach ($width in $widths) { $grid += "<w:gridCol w:w='$width'/>" }

    $borders = "<w:top w:val='single' w:sz='6' w:color='000000'/><w:left w:val='single' w:sz='6' w:color='000000'/>" +
               "<w:bottom w:val='single' w:sz='6' w:color='000000'/><w:right w:val='single' w:sz='6' w:color='000000'/>" +
               "<w:insideH w:val='single' w:sz='6' w:color='000000'/><w:insideV w:val='single' w:sz='6' w:color='000000'/>"

    $xml = "<w:tbl><w:tblPr><w:tblStyle w:val='TableGrid'/><w:tblW w:w='$contentWidth' w:type='dxa'/>"
    $xml += "<w:tblBorders>$borders</w:tblBorders><w:tblLayout w:type='fixed'/></w:tblPr><w:tblGrid>$grid</w:tblGrid>"

    $xml += "<w:tr><w:trPr><w:tblHeader/></w:trPr>"
    for ($i = 0; $i -lt $header.Count; $i++) { $xml += CellXml $header[$i] $widths[$i] $true }
    $xml += '</w:tr>'

    foreach ($row in $rows) {
        $xml += '<w:tr>'
        for ($i = 0; $i -lt $row.Count; $i++) {
            $value = if ($i -lt $row.Count) { $row[$i] } else { '' }
            $xml += CellXml $value $widths[$i] $false
        }
        $xml += '</w:tr>'
    }

    $xml += '</w:tbl>'
    $xml += "<w:p><w:pPr><w:spacing w:after='120'/></w:pPr></w:p>"

    return $xml
}

# ----- field-driven contents lists --------------------------------------

function FieldListXml($instruction, $placeholder) {
    $xml = "<w:p><w:pPr><w:spacing w:line='276' w:lineRule='auto' w:after='120'/></w:pPr>"
    $xml += "<w:r><w:fldChar w:fldCharType='begin' w:dirty='true'/></w:r>"
    $xml += "<w:r><w:instrText xml:space='preserve'>$instruction</w:instrText></w:r>"
    $xml += "<w:r><w:fldChar w:fldCharType='separate'/></w:r>"
    $xml += RunXml $placeholder $false $true 24
    $xml += "<w:r><w:fldChar w:fldCharType='end'/></w:r></w:p>"

    return $xml
}

# ----- the document body ------------------------------------------------

$body = New-Object System.Text.StringBuilder

function Add($xml) { [void]$body.Append($xml) }

# Cover ------------------------------------------------------------------

Add (ParaXml 'HỌC VIỆN KỸ THUẬT MẬT MÃ' $null 'center' $true $false 26 60)
Add (ParaXml 'KHOA CÔNG NGHỆ THÔNG TIN' $null 'center' $true $false 26 800)
Add (ParaXml 'ĐỒ ÁN MÔN HỌC' $null 'center' $true $false 32 60)
Add (ParaXml 'PHÁT TRIỂN GAME TRÊN ANDROID' $null 'center' $true $false 32 900)
Add (ParaXml 'Đề tài' $null 'center' $false $false 26 60)
Add (ParaXml 'SOULBOUND GATE' $null 'center' $true $false 44 60)
Add (ParaXml 'Trò chơi nhập vai hành động 2D nhìn từ trên xuống' $null 'center' $false $true 26 1000)
Add (ParaXml 'Nhóm sinh viên thực hiện' $null 'center' $true $false 26 120)
Add (ParaXml 'Quang Ninh' $null 'center' $false $false 26 60)
Add (ParaXml 'Hong Phong' $null 'center' $false $false 26 1200)
Add (ParaXml 'Hà Nội, tháng 9 năm 2026' $null 'center' $false $true 26 0)

# The cover is its own section so that page numbering starts after it.
$coverSect = "<w:p><w:pPr><w:sectPr><w:pgSz w:w='$pageW' w:h='$pageH'/>" +
             "<w:pgMar w:top='$marginOther' w:right='$marginOther' w:bottom='$marginOther' w:left='$marginLeft' w:header='720' w:footer='720' w:gutter='0'/>" +
             '</w:sectPr></w:pPr></w:p>'
Add $coverSect

# Contents lists ----------------------------------------------------------

Add (ParaXml 'MỤC LỤC' $null 'center' $true $false 30 240)
Add (FieldListXml ' TOC \o "1-2" \h \z \u ' 'Bấm Ctrl+A rồi F9 để cập nhật mục lục.')

Add (ParaXml 'DANH MỤC HÌNH VẼ' 'Heading1NoTOC' 'center' $true $false 30 240)
Add (FieldListXml ' TOC \h \z \c "Hinh" ' 'Bấm Ctrl+A rồi F9 để cập nhật danh mục hình vẽ.')

Add (ParaXml 'DANH MỤC BẢNG BIỂU' 'Heading1NoTOC' 'center' $true $false 30 240)
Add (FieldListXml ' TOC \h \z \c "Bang" ' 'Bấm Ctrl+A rồi F9 để cập nhật danh mục bảng biểu.')

Add (ParaXml 'DANH MỤC TỪ VIẾT TẮT' 'Heading1NoTOC' 'center' $true $false 30 240)

$abbreviations = @(
    @('AI', 'Artificial Intelligence', 'Trí tuệ nhân tạo'),
    @('APK', 'Android Package', 'Tệp cài đặt ứng dụng Android'),
    @('FPS', 'Frames Per Second', 'Số khung hình mỗi giây'),
    @('GC', 'Garbage Collector', 'Bộ thu gom rác bộ nhớ'),
    @('HUD', 'Head-Up Display', 'Lớp thông tin hiển thị khi chơi'),
    @('JSON', 'JavaScript Object Notation', 'Định dạng dữ liệu dạng văn bản'),
    @('LTS', 'Long Term Support', 'Phiên bản được hỗ trợ dài hạn'),
    @('NPC', 'Non-Player Character', 'Nhân vật không do người chơi điều khiển'),
    @('RPG', 'Role-Playing Game', 'Trò chơi nhập vai'),
    @('SFX', 'Sound Effects', 'Hiệu ứng âm thanh'),
    @('UI', 'User Interface', 'Giao diện người dùng'),
    @('URP', 'Universal Render Pipeline', 'Quy trình kết xuất đồ hoạ phổ quát của Unity'),
    @('VFX', 'Visual Effects', 'Hiệu ứng hình ảnh')
)

Add (TableXml @(0.16, 0.42, 0.42) @('Từ viết tắt', 'Tiếng Anh', 'Nghĩa tiếng Việt') $abbreviations)

# Chapters ----------------------------------------------------------------

$figureCount = 0
$tableCount = 0

$pendingCaption = $null
$pendingWeights = $null
$pendingHeader = $null
$pendingRows = @()

function FlushTable() {
    if (-not $script:pendingCaption) { return }

    $script:tableCount++
    Add (CaptionXml 'Bảng' 'Bang' $script:pendingCaption)
    Add (TableXml $script:pendingWeights $script:pendingHeader $script:pendingRows)

    $script:pendingCaption = $null
    $script:pendingWeights = $null
    $script:pendingHeader = $null
    $script:pendingRows = @()
}

foreach ($line in [System.IO.File]::ReadAllLines($Content, [System.Text.Encoding]::UTF8)) {
    if (-not $line.Trim()) { continue }

    $split = $line.IndexOf('|')
    if ($split -lt 0) { continue }

    $kind = $line.Substring(0, $split)
    $rest = $line.Substring($split + 1)

    switch ($kind) {
        'H1' { FlushTable; Add (HeadingXml 1 $rest) }
        'H2' { FlushTable; Add (HeadingXml 2 $rest) }
        'P' { FlushTable; Add (ParaXml $rest) }
        'B' { FlushTable; Add (BulletXml $rest) }
        'FIG' {
            FlushTable
            $parts = $rest.Split('|')
            $figureCount++
            Add (ImageXml $parts[0])
            Add (CaptionXml 'Hình' 'Hinh' $parts[1])
        }
        'TBLCAP' { FlushTable; $script:pendingCaption = $rest }
        'TBLW' { $script:pendingWeights = @($rest.Split(',') | ForEach-Object { [double]$_ }) }
        'TBLH' { $script:pendingHeader = @($rest.Split(';')) }
        'TBLR' { $script:pendingRows += , @($rest.Split(';')) }
        default { }
    }
}

FlushTable

# References --------------------------------------------------------------

Add (HeadingXml 1 'TÀI LIỆU THAM KHẢO')

$references = @(
    'Unity Technologies, "Unity User Manual 2022.3 (LTS)", https://docs.unity3d.com/2022.3/Documentation/Manual/index.html, truy cập tháng 9/2026.',
    'Unity Technologies, "Universal Render Pipeline documentation", https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/index.html, truy cập tháng 9/2026.',
    'Unity Technologies, "Input System package documentation", https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/index.html, truy cập tháng 9/2026.',
    'Unity Technologies, "Cinemachine documentation", https://docs.unity3d.com/Packages/com.unity.cinemachine@2.9/manual/index.html, truy cập tháng 9/2026.',
    'Unity Technologies, "Tilemap và Tilemap Collider 2D", https://docs.unity3d.com/2022.3/Documentation/Manual/class-Tilemap.html, truy cập tháng 9/2026.',
    'Unity Technologies, "Publishing for Android", https://docs.unity3d.com/2022.3/Documentation/Manual/android-BuildProcess.html, truy cập tháng 9/2026.',
    'Unity Technologies, "Unity Test Framework", https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/index.html, truy cập tháng 9/2026.',
    'Unity Technologies, "Understanding optimization in Unity: managed memory", https://docs.unity3d.com/2022.3/Documentation/Manual/performance-managed-memory.html, truy cập tháng 9/2026.',
    'R. Nystrom, "Game Programming Patterns", Genever Benning, 2014.',
    'I. Millington, J. Funge, "Artificial Intelligence for Games", 3rd edition, CRC Press, 2019.',
    'Học viện Kỹ thuật Mật mã, Khoa Công nghệ thông tin, "Hướng dẫn chấm điểm đồ án môn học - Phát triển game trên Android", 2026.'
)

$index = 1
foreach ($reference in $references) {
    $props = "<w:spacing w:line='360' w:lineRule='auto' w:after='120'/><w:ind w:left='454' w:hanging='454'/><w:jc w:val='both'/>"
    Add ("<w:p><w:pPr>$props</w:pPr>$(RunXml ("[$index] " + $reference))</w:p>")
    $index++
}

# Final section properties: this one carries the page numbers.
$bodySect = "<w:sectPr><w:footerReference w:type='default' r:id='rIdFooter'/><w:pgSz w:w='$pageW' w:h='$pageH'/>" +
            "<w:pgMar w:top='$marginOther' w:right='$marginOther' w:bottom='$marginOther' w:left='$marginLeft' w:header='720' w:footer='720' w:gutter='0'/>" +
            "<w:pgNumType w:start='1'/></w:sectPr>"
Add $bodySect

# ----- parts -------------------------------------------------------------

$documentXml = "<?xml version='1.0' encoding='UTF-8' standalone='yes'?>" +
    "<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main' " +
    "xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships' " +
    "xmlns:wp='http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing' " +
    "xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main' " +
    "xmlns:pic='http://schemas.openxmlformats.org/drawingml/2006/picture'>" +
    "<w:body>$($body.ToString())</w:body></w:document>"

Write-Part "$work\word\document.xml" $documentXml

$stylesXml = @"
<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<w:styles xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'>
  <w:docDefaults>
    <w:rPrDefault><w:rPr>
      <w:rFonts w:ascii='Times New Roman' w:hAnsi='Times New Roman' w:eastAsia='Times New Roman' w:cs='Times New Roman'/>
      <w:sz w:val='26'/><w:szCs w:val='26'/><w:lang w:val='vi-VN'/>
    </w:rPr></w:rPrDefault>
    <w:pPrDefault><w:pPr><w:spacing w:after='120' w:line='360' w:lineRule='auto'/></w:pPr></w:pPrDefault>
  </w:docDefaults>
  <w:style w:type='paragraph' w:default='1' w:styleId='Normal'>
    <w:name w:val='Normal'/><w:qFormat/>
  </w:style>
  <w:style w:type='paragraph' w:styleId='Heading1'>
    <w:name w:val='heading 1'/><w:basedOn w:val='Normal'/><w:next w:val='Normal'/><w:qFormat/>
    <w:pPr><w:outlineLvl w:val='0'/><w:keepNext/></w:pPr>
    <w:rPr><w:b/><w:sz w:val='30'/><w:szCs w:val='30'/></w:rPr>
  </w:style>
  <w:style w:type='paragraph' w:styleId='Heading2'>
    <w:name w:val='heading 2'/><w:basedOn w:val='Normal'/><w:next w:val='Normal'/><w:qFormat/>
    <w:pPr><w:outlineLvl w:val='1'/><w:keepNext/></w:pPr>
    <w:rPr><w:b/><w:sz w:val='28'/><w:szCs w:val='28'/></w:rPr>
  </w:style>
  <w:style w:type='paragraph' w:styleId='Heading1NoTOC'>
    <w:name w:val='Heading 1 No TOC'/><w:basedOn w:val='Normal'/><w:next w:val='Normal'/>
    <w:pPr><w:pageBreakBefore/><w:keepNext/></w:pPr>
    <w:rPr><w:b/><w:sz w:val='30'/><w:szCs w:val='30'/></w:rPr>
  </w:style>
  <w:style w:type='paragraph' w:styleId='Caption'>
    <w:name w:val='caption'/><w:basedOn w:val='Normal'/><w:next w:val='Normal'/><w:qFormat/>
    <w:pPr><w:keepLines/><w:spacing w:after='240'/></w:pPr>
    <w:rPr><w:i/><w:sz w:val='24'/><w:szCs w:val='24'/></w:rPr>
  </w:style>
  <w:style w:type='paragraph' w:styleId='ListParagraph'>
    <w:name w:val='List Paragraph'/><w:basedOn w:val='Normal'/><w:qFormat/>
    <w:pPr><w:contextualSpacing/></w:pPr>
  </w:style>
  <w:style w:type='paragraph' w:styleId='TOC1'><w:name w:val='toc 1'/><w:basedOn w:val='Normal'/>
    <w:pPr><w:spacing w:after='60' w:line='276' w:lineRule='auto'/><w:tabs><w:tab w:val='right' w:leader='dot' w:pos='9071'/></w:tabs></w:pPr>
    <w:rPr><w:b/></w:rPr>
  </w:style>
  <w:style w:type='paragraph' w:styleId='TOC2'><w:name w:val='toc 2'/><w:basedOn w:val='Normal'/>
    <w:pPr><w:spacing w:after='60' w:line='276' w:lineRule='auto'/><w:ind w:left='340'/><w:tabs><w:tab w:val='right' w:leader='dot' w:pos='9071'/></w:tabs></w:pPr>
  </w:style>
  <w:style w:type='paragraph' w:styleId='TableOfFigures'><w:name w:val='table of figures'/><w:basedOn w:val='Normal'/>
    <w:pPr><w:spacing w:after='60' w:line='276' w:lineRule='auto'/><w:tabs><w:tab w:val='right' w:leader='dot' w:pos='9071'/></w:tabs></w:pPr>
  </w:style>
  <w:style w:type='table' w:styleId='TableGrid'>
    <w:name w:val='Table Grid'/>
    <w:tblPr><w:tblBorders>
      <w:top w:val='single' w:sz='6' w:color='000000'/><w:left w:val='single' w:sz='6' w:color='000000'/>
      <w:bottom w:val='single' w:sz='6' w:color='000000'/><w:right w:val='single' w:sz='6' w:color='000000'/>
      <w:insideH w:val='single' w:sz='6' w:color='000000'/><w:insideV w:val='single' w:sz='6' w:color='000000'/>
    </w:tblBorders></w:tblPr>
  </w:style>
</w:styles>
"@

Write-Part "$work\word\styles.xml" $stylesXml

$numberingXml = @"
<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<w:numbering xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'>
  <w:abstractNum w:abstractNumId='0'>
    <w:lvl w:ilvl='0'>
      <w:start w:val='1'/><w:numFmt w:val='bullet'/><w:lvlText w:val='&#8226;'/><w:lvlJc w:val='left'/>
      <w:pPr><w:ind w:left='720' w:hanging='360'/></w:pPr>
      <w:rPr><w:rFonts w:ascii='Times New Roman' w:hAnsi='Times New Roman' w:hint='default'/></w:rPr>
    </w:lvl>
  </w:abstractNum>
  <w:num w:numId='1'><w:abstractNumId w:val='0'/></w:num>
</w:numbering>
"@

Write-Part "$work\word\numbering.xml" $numberingXml

$settingsXml = @"
<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<w:settings xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'>
  <w:zoom w:percent='100'/>
  <w:defaultTabStop w:val='720'/>
  <w:updateFields w:val='true'/>
  <w:compat/>
</w:settings>
"@

Write-Part "$work\word\settings.xml" $settingsXml

$footerXml = "<?xml version='1.0' encoding='UTF-8' standalone='yes'?>" +
  "<w:ftr xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'>" +
  "<w:p><w:pPr><w:jc w:val='center'/><w:spacing w:after='0'/></w:pPr>" +
  "<w:fldSimple w:instr=' PAGE '>" + (RunXml '1' $false $false 24) + '</w:fldSimple>' +
  '</w:p></w:ftr>'

Write-Part "$work\word\footer1.xml" $footerXml

$documentRels = "<?xml version='1.0' encoding='UTF-8' standalone='yes'?>" +
  "<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'>" +
  "<Relationship Id='rIdStyles' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles' Target='styles.xml'/>" +
  "<Relationship Id='rIdSettings' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/settings' Target='settings.xml'/>" +
  "<Relationship Id='rIdNumbering' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/numbering' Target='numbering.xml'/>" +
  "<Relationship Id='rIdFooter' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer' Target='footer1.xml'/>" +
  ($script:imageRels -join '') + '</Relationships>'

Write-Part "$work\word\_rels\document.xml.rels" $documentRels

$packageRels = "<?xml version='1.0' encoding='UTF-8' standalone='yes'?>" +
  "<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'>" +
  "<Relationship Id='rId1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument' Target='word/document.xml'/>" +
  "<Relationship Id='rId2' Type='http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties' Target='docProps/core.xml'/>" +
  "<Relationship Id='rId3' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties' Target='docProps/app.xml'/>" +
  '</Relationships>'

Write-Part "$work\_rels\.rels" $packageRels

$now = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ssZ')
$coreXml = "<?xml version='1.0' encoding='UTF-8' standalone='yes'?>" +
  "<cp:coreProperties xmlns:cp='http://schemas.openxmlformats.org/package/2006/metadata/core-properties' " +
  "xmlns:dc='http://purl.org/dc/elements/1.1/' xmlns:dcterms='http://purl.org/dc/terms/' " +
  "xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>" +
  "<dc:title>Đồ án môn học - Soulbound Gate</dc:title>" +
  "<dc:creator>Quang Ninh, Hong Phong</dc:creator>" +
  "<cp:lastModifiedBy>Quang Ninh, Hong Phong</cp:lastModifiedBy>" +
  "<dcterms:created xsi:type='dcterms:W3CDTF'>$now</dcterms:created>" +
  "<dcterms:modified xsi:type='dcterms:W3CDTF'>$now</dcterms:modified>" +
  '</cp:coreProperties>'

Write-Part "$work\docProps\core.xml" $coreXml

$appXml = "<?xml version='1.0' encoding='UTF-8' standalone='yes'?>" +
  "<Properties xmlns='http://schemas.openxmlformats.org/officeDocument/2006/extended-properties' " +
  "xmlns:vt='http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes'>" +
  '<Application>Soulbound Gate report builder</Application></Properties>'

Write-Part "$work\docProps\app.xml" $appXml

$contentTypes = "<?xml version='1.0' encoding='UTF-8' standalone='yes'?>" +
  "<Types xmlns='http://schemas.openxmlformats.org/package/2006/content-types'>" +
  "<Default Extension='rels' ContentType='application/vnd.openxmlformats-package.relationships+xml'/>" +
  "<Default Extension='xml' ContentType='application/xml'/>" +
  "<Default Extension='png' ContentType='image/png'/>" +
  "<Override PartName='/word/document.xml' ContentType='application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml'/>" +
  "<Override PartName='/word/styles.xml' ContentType='application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml'/>" +
  "<Override PartName='/word/settings.xml' ContentType='application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml'/>" +
  "<Override PartName='/word/numbering.xml' ContentType='application/vnd.openxmlformats-officedocument.wordprocessingml.numbering+xml'/>" +
  "<Override PartName='/word/footer1.xml' ContentType='application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml'/>" +
  "<Override PartName='/docProps/core.xml' ContentType='application/vnd.openxmlformats-package.core-properties+xml'/>" +
  "<Override PartName='/docProps/app.xml' ContentType='application/vnd.openxmlformats-officedocument.extended-properties+xml'/>" +
  '</Types>'

Write-Part "$work\[Content_Types].xml" $contentTypes

# ----- validate every part parses, then zip ------------------------------

# Read through .NET rather than Get-Content: one of the part names contains
# square brackets, which PowerShell would treat as a wildcard.
foreach ($part in (Get-ChildItem $work -Recurse -Include *.xml, *.rels)) {
    try { [xml]([System.IO.File]::ReadAllText($part.FullName, [System.Text.Encoding]::UTF8)) | Out-Null }
    catch { throw "Malformed XML in $($part.Name): $($_.Exception.Message)" }
}

if (Test-Path $Out) { Remove-Item $Out -Force }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Entries are added one at a time rather than with CreateFromDirectory: on
# .NET Framework that helper writes Windows backslashes into the entry names,
# and a package with "word\document.xml" in it is not one Word will open.
$archive = [System.IO.Compression.ZipFile]::Open($Out, [System.IO.Compression.ZipArchiveMode]::Create)

try {
    $files = @("$work\[Content_Types].xml")
    $files += Get-ChildItem $work -Recurse -File |
              Where-Object { $_.Name -ne '[Content_Types].xml' } |
              ForEach-Object { $_.FullName }

    foreach ($file in $files) {
        $name = $file.Substring($work.Length + 1).Replace('\', '/')

        $entry = $archive.CreateEntry($name, [System.IO.Compression.CompressionLevel]::Optimal)
        $stream = $entry.Open()

        try {
            $bytes = [System.IO.File]::ReadAllBytes($file)
            $stream.Write($bytes, 0, $bytes.Length)
        }
        finally { $stream.Dispose() }
    }
}
finally { $archive.Dispose() }

# Read the package back and confirm the parts are where they should be.
$check = [System.IO.Compression.ZipFile]::OpenRead($Out)
try {
    $names = @($check.Entries | ForEach-Object { $_.FullName })

    foreach ($required in @('[Content_Types].xml', '_rels/.rels', 'word/document.xml', 'word/styles.xml')) {
        if ($names -notcontains $required) { throw "Package is missing $required" }
    }

    if ($names | Where-Object { $_ -match '\\' }) { throw 'Package contains backslash entry names' }
}
finally { $check.Dispose() }

$size = [math]::Round((Get-Item $Out).Length / 1KB, 1)
"wrote $Out ($size KB)"
"figures: $figureCount, tables: $tableCount, images embedded: $script:imageIndex"
