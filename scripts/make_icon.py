"""Build FrameForge's geometric F/bolt mark as PNG, SVG and a multi-size Windows ICO."""
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
assets = root / "src" / "FrameForge.App" / "Assets"
assets.mkdir(exist_ok=True)
scale = 4
image = Image.new("RGBA", (256 * scale, 256 * scale))
draw = ImageDraw.Draw(image)
draw.rounded_rectangle((4*scale, 4*scale, 252*scale, 252*scale), radius=57*scale, fill="#101d30", outline="#2a4658", width=3*scale)
mark = [(67,57),(194,57),(179,91),(106,91),(94,122),(157,122),(142,155),(82,155),(65,202),(29,202)]
draw.polygon([(x*scale,y*scale) for x,y in mark], fill="#5ee0ba")
draw.polygon([(179*scale,113*scale),(210*scale,113*scale),(181*scale,161*scale),(204*scale,161*scale),(150*scale,217*scale),(166*scale,171*scale),(144*scale,171*scale)], fill="#f1f5fc")
image.resize((256,256), Image.Resampling.LANCZOS).save(assets / "FrameForge.png")
image.save(assets / "FrameForge.ico", sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
(assets / "FrameForge.svg").write_text('''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256"><rect x="4" y="4" width="248" height="248" rx="57" fill="#101d30" stroke="#2a4658" stroke-width="3"/><path d="M67 57H194L179 91H106L94 122H157L142 155H82L65 202H29Z" fill="#5ee0ba"/><path d="M179 113H210L181 161H204L150 217L166 171H144Z" fill="#f1f5fc"/></svg>''', encoding="utf-8")
print("Created FrameForge icon: 7 ICO sizes, PNG and SVG")
