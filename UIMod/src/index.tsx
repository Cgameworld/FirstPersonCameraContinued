import { ModRegistrar } from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { Entity, selectedInfo } from "cs2/bindings";

const register: ModRegistrar = (moduleRegistry) => {

    const CustomMenuButton = () => {
        return <div>
            <button id="MapTextureReplacer-MainGameButton" className="button_ke4 button_ke4 button_h9N" onClick={() => trigger("fpc", "ActivateFPC")}>
                <div className="tinted-icon_iKo icon_be5" style={{ backgroundImage: 'url(coui://uil/Standard/VideoCamera.svg)', backgroundPositionX: '2rem', backgroundPositionY: '2rem', backgroundColor: 'rgba(255,255,255,0)', backgroundSize: '35rem 35rem' }}>
                </div>
            </button>
        </div>;
    }

    //listen and inject the item into the DOM manually, can't figure out how to put the button in the same row in the official UI system
    const observeAndAppend = (): void => {
        const targetNode: HTMLElement | null = document.querySelector('.info-layout_BVk');
        const config: MutationObserverInit = { childList: true, subtree: true };
        const callback = (mutationsList: MutationRecord[], observer: MutationObserver): void => {
            for (let mutation of mutationsList) {
                if (mutation.type === 'childList') {
                    let element: HTMLElement | null = document.querySelector('.actions-section_X1x');
                    if (element && !element.querySelector('div')) {
                        //console.log('Element .actions-section_X1x found:', element);
                        let div: HTMLDivElement = document.createElement('div');
                        div.innerHTML = `<button class="ok button_Z9O button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT button_Z9O button_ECf item_It6 item-mouse-states_Fmi item-selected_tAM item-focused_FuT button_xGY">
    <img class="icon_Tdt icon_soN icon_Iwk" src="coui://uil/Colored/ArrowRight.svg"></img>
</button>`;
                        let triggerButton = div.querySelector('button');
                        if (triggerButton) {
                            triggerButton.onclick = triggerFollowEntity;
                        }
                    
                        element.appendChild(div);
                        //console.log('New div appended:', div);
                        observer.disconnect();
                        //console.log('Observer disconnected');
                        break;
                    }
                }
            }
        };

        let observer: MutationObserver = new MutationObserver(callback);

        if (targetNode) {
            observer.observe(targetNode, config);
        }
    }
        
    const triggerFollowEntity = () => {
        console.log("2TEST BUTTON WORKS - triggerFollowEntity1")
        trigger("fpc", "EnterFollowFPC")
    }

    moduleRegistry.append('GameTopRight', CustomMenuButton);

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