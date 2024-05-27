import { ModRegistrar } from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { Entity, selectedInfo } from "cs2/bindings";
import { useLocalization } from "cs2/l10n";
import { VanillaComponentsResolver } from "../types/internal";
import ReactDOM from 'react-dom';

const register: ModRegistrar = (moduleRegistry) => {

    const { DescriptionTooltip } = VanillaComponentsResolver.instance;

    // Translation.
    function translate(key: string) {
        const { translate } = useLocalization();
        return translate(key);
    }

    let tooltipDescriptionFreeCamera: string | null;
    let tooltipDescriptionFollowCamera: string | null;

    const CustomMenuButton = () => {

        tooltipDescriptionFreeCamera = translate("FirstPersonCameraContinued.TooltipFreeCamera");
        tooltipDescriptionFollowCamera = translate("FirstPersonCameraContinued.TooltipFollowCamera");

        return <div>
            <DescriptionTooltip title="First Person Camera" description={tooltipDescriptionFreeCamera}> 
            <button id="MapTextureReplacer-MainGameButton" className="button_ke4 button_ke4 button_h9N" onClick={() => trigger("fpc", "ActivateFPC")}>
                <div className="tinted-icon_iKo icon_be5" style={{ backgroundImage: 'url(coui://uil/Standard/VideoCamera.svg)', backgroundPositionX: '2rem', backgroundPositionY: '2rem', backgroundColor: 'rgba(255,255,255,0)', backgroundSize: '35rem 35rem' }}>
                </div>
                </button>
            </DescriptionTooltip>
        </div>;
    }

    moduleRegistry.append('GameTopRight', CustomMenuButton);

  
    const middleSections$ = selectedInfo.middleSections$;

    //listen and inject the item into the DOM manually, can't figure out how to put the button in the same row in the official UI system
    const observeAndAppend = (): void => {
        const targetNode: HTMLElement | null = document.querySelector('.info-layout_BVk');
        const config: MutationObserverInit = { childList: true, subtree: true };
        const callback = (mutationsList: MutationRecord[], observer: MutationObserver): void => {
            for (let mutation of mutationsList) {
                if (mutation.type === 'childList') {
                    let element: HTMLElement | null = document.querySelector('.actions-section_X1x');
                    if (element && !middleSections$.value.some(x =>
                        x?.__Type === "Game.UI.InGame.LevelSection" as any ||
                        x?.__Type === "Game.UI.InGame.RoadSection" as any ||
                        x?.__Type === "Game.UI.InGame.ResidentsSection" as any ||
                        x?.__Type === "Game.UI.InGame.UpkeepSection" as any
                    )) {
                        //console.log('Element .actions-section_X1x found:', element);
                        let existingDiv: HTMLDivElement | null = element.querySelector('div.fpc-injected-div');
                        if (!existingDiv) {
                            let div: HTMLDivElement = document.createElement('div');
                            div.className = 'fpc-injected-div';
                            ReactDOM.render(FPVInfoWindowButton(), div);

                            // Insert after the first button in the .actions-section_X1x element
                            let firstButton: HTMLButtonElement | null = element.querySelector('button');
                            if (firstButton && firstButton.nextSibling) {
                                element.insertBefore(div, firstButton.nextSibling);
                            } else {
                                element.appendChild(div);
                            }

                            console.log('New div appended:', div);
                            observer.disconnect();
                            //console.log('Observer disconnected');
                            break;
                        }
                    }
                }
            }
        };

        let observer: MutationObserver = new MutationObserver(callback);

        if (targetNode) {
            observer.observe(targetNode, config);
        }
    }

    const FPVInfoWindowButton = () => {
        return (
            <DescriptionTooltip title="First Person Camera" description={tooltipDescriptionFollowCamera}> 
                <button style={{ marginLeft: '6rem', marginRight: '8rem' }} className="ok button_Z9O button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT button_Z9O button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT button_xGY" onClick={() => trigger("fpc", "EnterFollowFPC")}>
                    <img className="icon_Tdt icon_soN icon_Iwk" src="coui://uil/Colored/VideoCamera.svg"></img>
                </button>
            </DescriptionTooltip>
        );
    }


    var selectedEntity: Entity;
    selectedInfo.selectedEntity$.subscribe(SelectedEntityChanged);
    function SelectedEntityChanged(newEntity: Entity) {
        selectedEntity = newEntity;
        trigger("fpc", "SelectedEntity", newEntity);
    }

    //keep track of current selected entity - from plop the growables
    const selectedEntity$ = selectedInfo.selectedEntity$;
    let currentEntity: any = null;

    selectedEntity$.subscribe((entity) => {
        if (!entity.index) {
            currentEntity = null;
            
            return entity
        }
        if (currentEntity != entity.index) {
            currentEntity = entity.index
        }
        observeAndAppend();
        return entity;
    })


}

export default register;