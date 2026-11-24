import type { Combatant } from '#lib/arena/battle.ts';
import type { Element } from '#lib/arena/catalog.ts';
import type { Cue } from '#lib/arena/round-playback.ts';

/**
 * A cue as the stage draws it. Its key is new with every step, so
 * its animation plays once for the step it belongs to.
 */
export type StageCue = Cue & { key: string };

/**
 * A monster on the battle stage, with everything the stage shows
 * of it. The page works it out; the stage only draws it.
 */
export interface StageMonster {
	combatant: Combatant;
	/** The hit points it shows: behind the combatant's while a round plays. */
	hitPoints: number;
	/** The species' display name. */
	name: string;
	/** The species' element. */
	element: Element;
	/** Whose monster it is, in the viewer's words. */
	owner: string;
	/** Whether the viewer can pick this monster right now. */
	selectable: boolean;
	selected: boolean;
	/** The latest thing the round being played made it do or did to it. */
	cue?: StageCue;
	onselect: () => void;
}

/** One side of the stage: a row of monsters under its owner's label. */
export interface StageSide {
	/** Whose side it is, in the viewer's words. */
	label: string;
	monsters: StageMonster[];
}
