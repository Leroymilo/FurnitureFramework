from pathlib import Path
from json import load, JSONDecodeError, dumps

from decoder import JSONCDecoder
from manifest import Manifest
from type import FType
from properties.lights import Light

class FPack:
	def __init__(self, path: Path, manifest: Manifest):
		self.manifest = manifest

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
		
		print(f"Parsing {path}...")
		
		if ("Format" not in self.data):
			print("No Format in content")
			return

		if (self.data["Format"] == 3):
			print("Pack is already in Format 3")
			return
	
		self.furniture: dict[str, FType] = {}
		if ("Furniture" in self.data and type(self.data["Furniture"]) is dict):
			furn: dict = self.data["Furniture"]
			for key, value in furn.items():
				key = FType.replace_token(key)
				self.furniture[key] = FType(value)
		
		self.included: dict[str, IPack] = {}
		if ("Included" in self.data and type(self.data["Included"]) is dict):
			inc: dict = self.data["Included"]
			for key, value in inc.items():
				self.included[key] = IPack(value, manifest)
				if self.included[key].pack is None:
					print(f"Included pack \"{key}\" is invalid, skipping this pack.")

		print(f"Finished parsing {path}!")
	
	def migrate(self):
		self.seasonal = []

		Light.pack_path = self.manifest.folder

		for furn in self.furniture.values():
			furn.migrate()
			if furn.seasonal:
				image = furn.source_image
				if type(image) is str:
					self.seasonal.append(image)
				elif type(image) is dict:
					self.seasonal += list(image.values())
				elif type(image) is list:
					self.seasonal += image
	
		for ipack in self.included.values():
			if ipack.pack is None: continue
			ipack.pack.migrate()
			self.seasonal += ipack.pack.seasonal
		
		self.data["Format"] = 3
	
	def save(self, path: Path):
		self.data["Furniture"] = {key: furn.data for key, furn in self.furniture.items()}
		self.data["Included"] = {key: includ.save() for key, includ in self.included.items()}
		path.parent.mkdir(exist_ok=True, parents=True)
		path.write_text(dumps(self.data, indent='\t'))

class IPack:
	def __init__(self, data: dict, manifest: Manifest):
		self.data = data
		self.pack = None
		self.manifest = manifest

		if type(data) is not dict: return
		if "Path" not in data: return

		self.pack = FPack(manifest.folder / data["Path"], manifest)
	
	def save(self):
		self.pack.save(self.manifest.folder/Path(self.data["Path"]))
		return self.data