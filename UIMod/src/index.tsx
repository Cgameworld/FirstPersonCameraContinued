import React, { useState, useEffect } from 'react';
import { ModRegistrar } from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { Entity, selectedInfo } from "cs2/bindings";
import { useLocalization } from "cs2/l10n";
import { VanillaComponentsResolver } from "../types/internal";
import ReactDOM from 'react-dom';
import 'style/DropdownWindow.scss';
import 'style/Crosshair.scss';
import engine from 'cohtml/cohtml';

const register: ModRegistrar = (moduleRegistry) => {

    const { DescriptionTooltip } = VanillaComponentsResolver.instance;

    // Translation.
    function translate(key: string) {
        const { translate } = useLocalization();
        return translate(key);
    }

    let tooltipDescriptionMainCameraIcon: string | null;
    let tooltipDescriptionFollowCamera: string | null;

    let uiTextEnterFreeCamera: string | null;
    let uiTextFollowRandomCim: string | null;
    let uiTextFollowRandomVehicle: string | null;

    const ShowCrosshair$ = bindValue<boolean>('fpc', 'ShowCrosshair');
    const FollowedEntityInfo$ = bindValue<string>('fpc', 'FollowedEntityInfo');

    const CustomMenuButton = () => {

        const [showButtonDropdown, setShowButtonDropdown] = useState(false);

        const toggleButtonDropdown = () => {
            setShowButtonDropdown(!showButtonDropdown);
        };


        const showCrosshair = useValue(ShowCrosshair$);
        const followedEntityInfo = useValue(FollowedEntityInfo$);

        //const showCrosshair: boolean = true;

        tooltipDescriptionMainCameraIcon = translate("FirstPersonCameraContinued.TooltipMainCameraIcon");
        tooltipDescriptionFollowCamera = translate("FirstPersonCameraContinued.TooltipFollowCamera");

        uiTextEnterFreeCamera = translate("FirstPersonCameraContinued.EnterFreeCamera");
        uiTextFollowRandomCim = translate("FirstPersonCameraContinued.FollowRandomCim");
        uiTextFollowRandomVehicle = translate("FirstPersonCameraContinued.FollowRandomVehicle");

        var selectedEntity: Entity;
        if (selectedInfo && selectedInfo.selectedEntity$) {
            selectedInfo.selectedEntity$.subscribe(SelectedEntityChanged);
        }
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

        useEffect(() => {
            if (showButtonDropdown) {
                //const mainGameButton = document.querySelector('#FPC-MainGameButton');
                const mainGameButton = document.querySelector('.main-container__E2');
                if (mainGameButton && mainGameButton.parentNode) {
                    const dropdownRoot = document.createElement('div');
                    dropdownRoot.id = 'top-right-layout_sSC';

                    mainGameButton.parentNode.appendChild(dropdownRoot);

                    ReactDOM.render(<DropdownWindow onClose={toggleButtonDropdown} />, dropdownRoot);

                    return () => {
                        ReactDOM.unmountComponentAtNode(dropdownRoot);
                        if (dropdownRoot.parentNode) {
                            dropdownRoot.parentNode.removeChild(dropdownRoot);
                        }
                    };
                }
            }
        }, [showButtonDropdown]);


        useEffect(() => {
            if (showCrosshair) {
                const div = document.querySelector('.game-main-screen_TRK.child-opacity-transition_nkS');

                const crosshairX = document.createElement('div');
                crosshairX.id = "crosshairX-fpc";

                const crosshairY = document.createElement('div');
                crosshairY.id = "crosshairY-fpc";

                div?.appendChild(crosshairX);
                div?.appendChild(crosshairY);
            }
            else {
                document.getElementById('crosshairX-fpc')?.remove();
                document.getElementById('crosshairY-fpc')?.remove();
            }

        }, [showCrosshair]);

        const parsedSpeed: number = JSON.parse(followedEntityInfo).currentSpeed
        const parsedUnits: number = JSON.parse(followedEntityInfo).unitsSystem;
        const parsedPassengers: number = JSON.parse(followedEntityInfo).passengers;
        const vehicleType: string = JSON.parse(followedEntityInfo).vehicleType;

        let formattedSpeed: string;

        if (parsedUnits == 1) {
            formattedSpeed = Math.round(parsedSpeed * 1.26) + " mph";
        }
        else {
            formattedSpeed = Math.round(parsedSpeed * 1.8) + " km/h";
        }

        return <div>
            <DescriptionTooltip title="First Person Camera" description={tooltipDescriptionMainCameraIcon}>
                <button id="FPC-MainGameButton" className="button_ke4 button_ke4 button_h9N" onClick={() => {
                    engine.trigger("audio.playSound", "select-item", 1);
                    toggleButtonDropdown();
                }}>
                    <div className="tinted-icon_iKo icon_be5" style={{ backgroundImage: 'url(coui://uil/Standard/VideoCamera.svg)', backgroundPositionX: '1rem', backgroundPositionY: '1rem', backgroundColor: 'rgba(255,255,255,0)', backgroundSize: '35rem 35rem' }}>
                    </div>
                </button>
            </DescriptionTooltip>
            <div style={{ backgroundColor: 'rgba(255,255,255,0.7)' }}>
                {parsedSpeed !== -1 && <div>Speed: {formattedSpeed}</div>}
                {parsedPassengers !== -1 && <div>Passengers: {parsedPassengers}</div>}
                <div>Vehicle Type: {vehicleType}</div>
            </div>
        </div>;
    }

    moduleRegistry.append('GameTopRight', CustomMenuButton);

    const middleSections$ = selectedInfo.middleSections$;
    const titleSection$ = selectedInfo.titleSection$;

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
                    ) &&
                        !JSON.stringify(titleSection$.value?.name).includes("Decal")
                    ) {
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

    const DropdownWindow: React.FC<{ onClose: () => void }> = ({ onClose }) => {

        const clickedDropdownItem = (item: string) => {
            onClose();
            engine.trigger("audio.playSound", "select-item", 1);
            trigger("fpc", item);
        };

        //dynamically change width of dropdown window based on locale
        const [dropdownWidth, setDropdownWidth] = useState<string>('220rem');
        const [rightOffset, setRightOffset] = useState<string>('0rem');

        useEffect(() => {
            const texts = [uiTextEnterFreeCamera, uiTextFollowRandomCim, uiTextFollowRandomVehicle];

            const calculateWidth = () => {

                //check if text has zh-HANS, zh-HANT, ko, jp characters
                if (!texts.some(text =>
                    text && /[\u3000-\u303f\u3040-\u309f\u30a0-\u30ff\uff00-\uff9f\u4e00-\u9faf\u3400-\u4dbf]/.test(text))) {
                    const longestText = Math.max(
                        ...(texts.map(text => text?.length || 0))
                    );
                    const calculatedWidth = longestText * 10 + 15;
                    setDropdownWidth(`${calculatedWidth}rem`);
                }
            };

            const updateRightOffset = () => {
                const button = document.querySelector('#FPC-MainGameButton');
                if (button) {
                    const rect = button.getBoundingClientRect();
                    const offset = window.innerWidth - rect.left;
                    const offsetAdjusted = offset / (window.innerWidth / 1920) - 40;
                    setRightOffset(`${offsetAdjusted}rem`);
                }
            };

            calculateWidth();
            updateRightOffset();
        }, [uiTextEnterFreeCamera, uiTextFollowRandomCim, uiTextFollowRandomVehicle]);

        return (
            <div style={{ width: dropdownWidth, right: rightOffset }} className="fpc-dropdownpanel panel_YqS expanded collapsible advisor-panel_dXi advisor-panel_mrr top-right-panel_A2r">
                <div className="content_XD5 content_AD7 child-opacity-transition_nkS">
                    <div className="scrollable_DXr y_SMM scrollable_wt8">
                        <div className="content_gqa" style={{ padding: '0' }} >
                            <div className="infoview-panel-section_RXJ" style={{ padding: '0' }}>
                                <div className="content_1xS focusable_GEc item-focused_FuT" style={{ padding: '0' }}>
                                    <div className="row_S2v fpc-right-row" onClick={() => clickedDropdownItem("ActivateFPC")}>
                                        <div className="right_k3O row_S2v">{uiTextEnterFreeCamera}</div>
                                    </div>
                                    <div className="row_S2v fpc-right-row" onClick={() => clickedDropdownItem("RandomCimFPC")}>
                                        <div className="right_k3O row_S2v">{uiTextFollowRandomCim}</div>
                                    </div>
                                    <div className="row_S2v fpc-right-row" onClick={() => clickedDropdownItem("RandomVehicleFPC")}>
                                        <div className="right_k3O row_S2v">{uiTextFollowRandomVehicle}</div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        );
    };
}

export default register;