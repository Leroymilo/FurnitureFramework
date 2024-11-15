from pathlib import Path
from json import load, JSONDecodeError

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