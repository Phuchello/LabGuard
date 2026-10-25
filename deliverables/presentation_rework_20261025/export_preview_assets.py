from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from reportlab.pdfgen import canvas
from reportlab.lib.utils import ImageReader

out = Path(__file__).resolve().parent
slides = [out / f"LabGuard_Nhom5_GK_Slide_{i:02d}.png" for i in range(1, 10)]
for p in slides:
    if not p.exists():
        raise FileNotFoundError(p)

pdf_path = out / "LabGuard_Nhom5_GK_Keynote_REWORK.pdf"
page_w, page_h = 960, 540
pdf = canvas.Canvas(str(pdf_path), pagesize=(page_w, page_h), pageCompression=1)
for p in slides:
    pdf.drawImage(ImageReader(str(p)), 0, 0, width=page_w, height=page_h, preserveAspectRatio=True, anchor='c', mask='auto')
    pdf.showPage()
pdf.save()

thumb_w, thumb_h = 640, 360
label_h, gap, margin = 36, 28, 40
sheet_w = margin * 2 + 3 * thumb_w + 2 * gap
sheet_h = margin * 2 + 3 * (thumb_h + label_h) + 2 * gap
sheet = Image.new('RGB', (sheet_w, sheet_h), '#F5F4F0')
draw = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype('arial.ttf', 18)
except OSError:
    font = ImageFont.load_default()
for idx, p in enumerate(slides):
    r, c = divmod(idx, 3)
    x = margin + c * (thumb_w + gap)
    y = margin + r * (thumb_h + label_h + gap)
    im = Image.open(p).convert('RGB').resize((thumb_w, thumb_h), Image.Resampling.LANCZOS)
    sheet.paste(im, (x, y))
    draw.rectangle((x, y + thumb_h, x + thumb_w, y + thumb_h + label_h), fill='#101C2B')
    draw.text((x + 14, y + thumb_h + 8), f"SLIDE {idx+1:02d}", fill='#83D0C6', font=font)
    draw.text((x + 120, y + thumb_h + 8), ["Vấn đề", "MAY-03", "Topology", "TCP framing", "SQL + ACK", "Admin", "Tiến độ", "Lộ trình", "Kết luận"][idx], fill='#FFFFFF', font=font)
contact = out / "LabGuard_Nhom5_GK_Contact_Sheet.png"
sheet.save(contact, optimize=True)
print(f"PDF={pdf_path} ({pdf_path.stat().st_size} bytes)")
print(f"CONTACT={contact} ({contact.stat().st_size} bytes, {sheet_w}x{sheet_h})")
