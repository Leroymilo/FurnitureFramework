from pathlib import Path
from shutil import copytree
from json import dumps

from manifest import Manifest
from pack import FPack

class Mod:
	def __init__(self, folder: Path):
		self.manifest = Manifest(folder)
		if self.manifest.data is None: return

		self.content = FPack(folder / "content.json", self.manifest)
		if self.content.data is None: return
	
	def migrate(self):
		self.manifest.migrate()
		self.content.migrate()
	
	def save(self, out: Path):

		if len(self.content.seasonal) > 0:
			self.make_cp(out / "[CP]")
			out /= "[FF]"

		copytree(self.manifest.folder / "assets", out / "assets", dirs_exist_ok=True)

		out.mkdir(parents=True, exist_ok=True)
		self.manifest.save(out / "manifest.json")
		self.content.save(out / "content.json")
	
	def make_cp(self, out: Path):
		self.manifest.save_cp(out / "manifest.json")

		cp_data = {
			"Format": "2.5.0",
			"Changes": []
		}

		mod_id: str = self.manifest.data["UniqueID"]

		for image_str in self.content.seasonal:
			image_path = Path(image_str)
			new_file = image_path.stem + "_{{season}}" + image_path.suffix
			change = {
				"LogName": f"Seasonal texture for FF/{mod_id}/{image_path}",
				"Action": "Load",
				"Target": f"FF/{mod_id}/{image_path.parent}/{image_path.stem}",
				"FromFile": f"{image_path.parent}/{new_file}"
			}
			cp_data["Changes"].append(change)

			for season in ["spring", "summer", "fall", "winter"]:
				seasonal_path = image_path.parent / Path(image_path.stem + '_' + season + image_path.suffix)
				(out / seasonal_path).parent.mkdir(exist_ok=True, parents=True)
				(self.manifest.folder / seasonal_path).rename(out / seasonal_path)
		
		(out / "content.json").write_text(dumps(cp_data, indent='\t'))
		self.manifest.save_cp(out / "manifest.json")

		# create the actual Content Pack and copy the required textures