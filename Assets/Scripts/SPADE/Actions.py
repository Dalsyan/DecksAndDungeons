import json
from socket import socket as s

from spade.behaviour import *

import AgenteCarta as card
import OwlOntology

from cmath import sqrt
from typing import Tuple

from owlready2 import *

FREE = False
OCCUPIED = True

class Actions:
    def __init__(self, spade_socket  = None, unity_socket = None , owl = OwlOntology.OntologyActions):
        self.spade_sock = spade_socket
        self.unity_sock = unity_socket
        self.owl = owl()

    ##############################
    #                            #
    #           CARTAS           #
    #                            #
    ##############################
    
    async def nearest_enemy(self, player_card_agent : card.CardAgent, enemy_card_agents : list):
        nearest = None

        for agent in enemy_card_agents:
            if nearest == None:
                nearest = agent

            if self.dist(player_card_agent, agent) < self.dist(player_card_agent, nearest):
                nearest = agent
                
        print(f"{player_card_agent.name} nearest_enemy: {nearest.name}")
        return nearest

    async def process_pos(self, pos_str):
        pos_tuple = eval(pos_str)

        return pos_tuple

    async def dist(self, card_agent : card.CardAgent, other_card_agent : card.CardAgent):
        agent_pos = self.process_pos(card_agent.pos)
        other_pos = self.process_pos(other_card_agent.pos)

        x1, y1 = agent_pos[0], agent_pos[1]
        x2, y2 = other_pos[0], other_pos[1]
        
        dist = sqrt((x2-x1)**2+(y2-y1)**2)

        print(f"{card_agent.name} distance with {other_card_agent.name}: {dist}")
        return dist

    async def can_move(self, table : dict, pos : str):
        if table[pos] == OCCUPIED:
            return False
        else: 
            return True

    async def move(self, player_card_agent : card.CardAgent, table : dict, new_pos : str):
        if self.can_move(table, new_pos):
            table[player_card_agent.pos] = FREE
            player_card_agent.pos = new_pos
            table[new_pos] = OCCUPIED

    async def attack(self, player_card_agent : card.CardAgent, enemy_card_agent : card.CardAgent, damage : int, special : bool):
        if self.dist(player_card_agent, enemy_card_agent) <= player_card_agent.range:
            if special:
                damage = damage + player_card_agent.level

            if not enemy_card_agent.shielded:
                enemy_card_agent.current_hp = enemy_card_agent.current_hp - damage
            else:
                enemy_card_agent.shielded = False

    async def shield(self, player_card_agent : card.CardAgent, ally_card_agent : card.CardAgent):
        if self.Dist(player_card_agent, ally_card_agent) <= player_card_agent.range:
            ally_card_agent.shielded = True

    async def heal(self, player_card_agent : card.CardAgent, ally_card_agent : card.CardAgent, heal : int):
        if self.dist(player_card_agent, ally_card_agent) <= player_card_agent.range and ally_card_agent.current_hp < ally_card_agent.hp:
            healed_hp = 0
            while healed_hp < heal:
                ally_card_agent.current_hp += 1

    ##############################
    #                            #
    #          SOCKETS           #
    #                            #
    ##############################
    
    async def send_message_to_socket(self, msg : str):
        encoded_msg = (msg).encode()
        self.unity_sock.sendall(bytearray(encoded_msg))

    async def send_action_to_socket(self, msg : dict):
        encoded_msg = json.dumps(msg).encode()
        self.unity_sock.sendall(bytearray(encoded_msg))

    ##############################
    #                            #
    #          ONTOLOGY          #
    #                            #
    ##############################

    def get_card_role(self, card):
        role = self.owl.get_card_role(card)
        return role

    def order_cards_by_prio(self, cards : list[card.CardAgent]):
        res = cards.sort(Key = lambda x : x.prio, reverse = True)
        return res

    def search_for_card(self, name):
        card = self.owl.search_for_card(name)
        return card
        
    def search_for_deck(self, name):
        deck = self.owl.search_for_deck(name)
        return deck

    def card_to_json_action(self, card):
        card_json = self.owl.card_to_dict(card)

        return card_json

    def deck_to_json_action(self, sender, deck):
        deck_json = {'action': sender, 'data': self.owl.deck_to_list(deck) }

        return deck_json

    def create_deck(self):
        deck = self.owl.create_deck()

        return deck