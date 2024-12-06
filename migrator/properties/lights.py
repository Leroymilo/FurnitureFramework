from copy import deepcopy

class Light:
	light_number = 0	# for making new light sprites

	def __init__(self, data: dict, rot_name: str, base_height: int):
		if rot_name in data:
			self = Light(data[rot_name], rot_name, base_height)
			return
		
		self.data = deepcopy(data)

		self.data["Position"]["Y"] -= base_height

		# TODO: read Source Image to make Source Rectangle

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
		
			self.data["Mode (deprecated, update sprite to fix)"] = self.data["Mode"]
			self.data.pop("Mode")
	
	def make_toggle(self, when_on: bool):
		# TODO
		# open source image, make new image with double width
		# if when_on, then put original image on the left
		# else, then put original image on the right
		pass

	def make_timed(self, when_dark: bool):
		# TODO
		# open source image, make new image with double height
		# if when_dark, then put original image on the top
		# else, then put original image on the bottom
		pass

class Lights:
	
	def __init__(self, data: dict | list, rot_name: str, base_height: int):
		self.data = []
		if type(data) is list:
			self.data = [Light(data[i], rot_name, base_height) for i in range(data)]
		
		elif type(data) is dict:
			if rot_name in data:
				self = Lights(data[rot_name], rot_name, base_height)
				return
			
			self.data = [Light(data, rot_name, base_height)]
		
	def to_json(self):
		return [light.data for light in self.data]