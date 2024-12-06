from copy import deepcopy

class Particle:
	def __init__(self, data: dict, rot_name: str, base_height: int):
		if rot_name in data:
			self = Particle(data[rot_name], rot_name, base_height)
			return
		
		self.data = deepcopy(data)
		self.data["Spawn Rect"]["Y"] -= base_height


class Particles:

	def __init__(self, data: dict | list, rot_name: str, base_height: int):
		self.data = []
		if type(data) is list:
			self.data = [Particle(data[i], rot_name, base_height) for i in range(data)]
		
		elif type(data) is dict:
			if rot_name in data:
				self = Particles(data[rot_name], rot_name, base_height)
				return
			
			self.data = [Particle(data, rot_name, base_height)]

	def to_json(self):
		return [layer.data for layer in self.data]