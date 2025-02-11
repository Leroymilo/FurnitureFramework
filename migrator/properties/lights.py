from copy import deepcopy
from pathlib import Path

from PIL import Image, UnidentifiedImageError, ImageChops

from structs import Rectangle

class Light:
	to_save: dict[str, Image.Image] = {}

	pack_path: Path = None
	sv_path: Path = None
	ff_path: Path = None

	def __init__(self, data: dict, rot_name: str, base_height: int):
		if rot_name in data:
			data = data[rot_name]
		
		self.data = deepcopy(data)

		self.data["Position"]["Y"] -= base_height

		self.image_path: str = self.data["Source Image"]
		self.image = self.get_image()
		if "Source Rect" in self.data:
			self.rect = Rectangle(self.data["Source Rect"], rot_name)
		else:
			self.rect = Rectangle({
				"X": 0, "Y": 0,
				"Width": self.image.size[0],
				"Height": self.image.size[1]
			}, rot_name)

		if "Mode" in self.data:
			match self.data["Mode"]:
				case "always_on":
					pass
				case "when_on":
					self.data["Toggle"] = True
					self.make_toggle(True)
				case "when_off":
					self.data["Toggle"] = True
					self.make_toggle(False)
				case "when_dark_out":
					self.data["Time Based"] = True
					self.make_timed(True)
				case "when_bright_out":
					self.data["Time Based"] = True
					self.make_timed(False)
			self.data.pop("Mode")
		
		if "Is Glow" in self.data and self.data["Is Glow"]:
			self.data["Light Type"] = "Glow"
		else:
			self.data["Light Type"] = "Source"
		if "Is Glow" in self.data:
			self.data.pop("Is Glow")
		
		for key, value in Light.to_save.items():
			if len(set(ImageChops.difference(self.image, value).getdata())) == 0:
				# Image is already being saved
				self.data["Source Image"] = key
				break
		else:
			if self.image_path is None:
				self.image_path = f"assets/lights/light_{len(Light.to_save)}.png"
			self.data["Source Image"] = self.image_path
			Light.to_save[self.image_path] = self.image
		
		self.data["Source Rect"] = self.rect.data

	def get_image(self) -> Image.Image:
		if self.image_path.startswith("FF/"):
			if Light.ff_path is None:
				txt = input("A Light Source Image is taken from FF's files, please give the absolute path to the FF mod folder:\n")
				Light.ff_path = Path(txt)
				while not Light.ff_path.exists():
					print("Type \"skip\" to skip this, it will ignore this light.")
					txt = input("This path does not exist, please provide the absolute path to the FF mod folder:\n")
					Light.ff_path = Path(txt)
			path = Light.ff_path / self.image_path[3:]
			self.image_path = None
		
		elif self.image_path.startswith("Content/"):
			if Light.sv_path is None:
				txt = input("A Light Source Image is taken from the game's files, please give the absolute path into \"Stardew Valley/Content (unpacked)\":\n")
				Light.sv_path = Path(txt)
				while True:
					if not Light.sv_path.exists():
						print("Type \"skip\" to skip this, it will ignore this light.")
						txt = input("This path does not exist, please provide the absolute path into \"Stardew Valley/Content (unpacked)\":\n")
					elif (Light.sv_path.parent.name, Light.sv_path.name) != ("Stardew Valley", "Content (unpacked)"):
						print("Type \"skip\" to skip this, it will ignore this light.")
						txt = input("This path does not end in the unpacked content folder, please provide the absolute path into \"Stardew Valley/Content (unpacked)\":\n")
					else: break
					Light.sv_path = Path(txt)
					
			path = Light.sv_path / self.image_path[8:]
			self.image_path = None
		
		else:
			path = Light.pack_path / self.image_path

		print("reading light image at", path)
		return Image.open(path)

	def make_toggle(self, when_on: bool):
		# make new image with double width
		# if when_on, then put original image on the right
		# else, then put original image on the left

		w, h = (self.rect.data["Width"], self.rect.data["Height"])
		if when_on: x = w
		else: x = 0

		new_image: Image.Image = Image.new("RGBA", (w*2, h), color=(0, 0, 0, 0))
		new_image.paste(self.image.crop(self.rect.get_box()), (x, 0, x+w, h))
		self.image = new_image
		self.rect = Rectangle({"X": 0, "Y": 0, "Width": w, "Height": h})

	def make_timed(self, when_dark: bool):
		# make new image with double height
		# if when_dark, then put original image on the bottom
		# else, then put original image on the top

		w, h = (self.rect.data["Width"], self.rect.data["Height"])
		if when_dark: y = h
		else: y = 0

		new_image: Image.Image = Image.new("RGBA", (w, h*2), color=(0, 0, 0, 0))
		new_image.paste(self.image.crop(self.rect.get_box()), (0, y, w, y+h))
		self.image = new_image
		self.rect = Rectangle({"X": 0, "Y": 0, "Width": w, "Height": h})

class Lights:
	
	def __init__(self, data: dict | list, rot_name: str, base_height: int):
		self.data = []
		if type(data) is dict and rot_name in data:
			data = data[rot_name]

		if type(data) is list:
			for val in data:
				try:
					self.data.append(Light(val, rot_name, base_height))
				except FileNotFoundError or UnidentifiedImageError as e:
					print(f"Could not read the Source Image of a Light in {data}:\n{e}\nskipping light.")
		
		elif type(data) is dict:
			self.data = [Light(data, rot_name, base_height)]
		
	def to_json(self):
		return [light.data for light in self.data]