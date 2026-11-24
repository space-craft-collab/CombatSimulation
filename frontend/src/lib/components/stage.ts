import type { Combatant } from '#lib/arena/battle.ts';
import type { Element } from '#lib/arena/catalog.ts';

/**
 * A monster on the battle stage, with everything the stage shows
 * of it. The page works it out; the stage only draws it.
 */
export interface StageMonster {
	combatant: Combatant;
	/** The species' display name. */
	name: string;
	/** The species' element. */
	element: Element;
	/** Whose monster it is, in the viewer's words. */
	owner: string;
	/** Whether the viewer can pick this monster right now. */
	selectable: boolean;
	selected: boolean;
	/** What the round just played did to it, for a short flash. */
	lastChange?: { round: number; damage: number; healing: number };
	onselect: () => void;
}

/** One side of the stage: a row of monsters under its owner's label. */
export interface StageSide {
	/** Whose side it is, in the viewer's words. */
	label: string;
	monsters: StageMonster[];
}
