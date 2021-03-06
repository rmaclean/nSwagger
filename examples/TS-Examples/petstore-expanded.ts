//nSwaggerVersion:0.0.6
// This file was automatically generated by nSwagger. Changes made to this file will be lost if nSwagger is run again. See https://github.com/rmaclean/nswagger for more information.
// This file was last generated at: 2016-04-08T12:38:56.7476500Z
namespace nSwagger {
    export module SwaggerPetstore {
        export interface IPet {
        }

        export interface INewPet {
            name: string;
            tag: string;
        }

        export interface IError {
            code: number;
            message: string;
        }

        export interface IfindPetsRequest {
            tags?: Array<string>;
            limit?: number;
        }

        export interface IaddPetRequest {
            pet: INewPet;
        }

        export interface IdeletePetRequest {
            id: number;
        }

        export interface IfindpetbyidRequest {
            id: number;
        }

        export interface API {
            setToken(value: string, headerOrQueryName: string, isQuery: boolean): void;
            findPets(parameters?: IfindPetsRequest): PromiseLike<Array<IPet>>;
            addPet(parameters: IaddPetRequest): PromiseLike<IPet>;
            deletePet(parameters: IdeletePetRequest): PromiseLike<void>;
            findpetbyid(parameters: IfindpetbyidRequest): PromiseLike<IPet>;
        }
    }
}
