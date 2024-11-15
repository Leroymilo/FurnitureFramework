from json import load, JSONDecodeError

from decoder import JSONCDecoder
from manifest import Manifest

class FPack:
	def __init__(self, path: str, manifest: Manifest):

		try:
			with open(path) as f:
				self.data = load(f, cls=JSONCDecoder)
		except OSError:
			print(f"Missing {path}!")
			self.data = None
			return
		except JSONDecodeError as e:
			print(f"JSON at {path} could not be loaded: {e}")
			self.data = None
			return
		
		print(f"Parsing {path}...")
		
		if ("Format" not in self.data):
			print("No Format in content")
			return

		if (self.data["Format"] == 3):
			print("Pack is already in Format 3")
			return
	
		print(f"Finished parsing {path}!")