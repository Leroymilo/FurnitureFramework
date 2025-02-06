from pathlib import Path
from json import load, JSONDecodeError, dumps
from copy import deepcopy

from decoder import JSONCDecoder

class Manifest:
	def __init__(self, folder: Path):
		self.folder = folder
		path = folder / "manifest.json"

		self.data = None
		
		try:
			with open(path) as f:
				self.data = load(f, cls=JSONCDecoder)
		except OSError:
			print(f"Missing {path}!")
			return
		except JSONDecodeError as e:
			print(f"JSON at {path} could not be loaded: {e}")
			return
	
	def migrate(self):
		# TODO: update MinimumApiVersion and MinimumGameVersion
		
		# update ContentPackFor
		self.data["ContentPackFor"] = {
			"UniqueID": "leroymilo.FurnitureFramework",
			"MinimumVersion": "3.0.0"
		}
	
	def save(self, path: Path):
		self.folder = path.parent
		path.write_text(dumps(self.data, indent='\t'))
	
	def save_cp(self, path: Path):
		cp_data = deepcopy(self.data)

		id: str = cp_data["UniqueID"]
		if "FF" in id: id = id.replace("FF", "CP")
		else: id += "CP"
		cp_data["UniqueID"] = id

		cp_data["ContentPackFor"] = {
			"UniqueID": "Pathoschild.ContentPatcher",
			"MinimumVersion": "2.5"
		}

		path.parent.mkdir(exist_ok=True, parents=True)
		path.write_text(dumps(cp_data, indent='\t'))